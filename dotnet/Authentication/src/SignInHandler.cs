namespace Hirameku.Authentication;

using AutoMapper;
using FluentValidation;
using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Common.Service;
using Hirameku.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NLog;

public class SignInHandler : ISignInHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public SignInHandler(
        IDocumentDao<AuthenticationEvent> authenticationEventDao,
        IOptions<AuthenticationOptions> authenticationOptions,
        ICacheClient cache,
        ICachedValueDao cachedValueDao,
        IMapper mapper,
        IPasswordDao passwordDao,
        IPersistentTokenIssuer persistentTokenIssuer,
        ISecurityTokenIssuer securityTokenIssuer,
        IValidator<SignInModel> signInModelValidator,
        IDocumentDao<UserDocument> userDao)
    {
        this.AuthenticationEventDao = authenticationEventDao;
        this.AuthenticationOptions = authenticationOptions;
        this.Cache = cache;
        this.CachedValueDao = cachedValueDao;
        this.Mapper = mapper;
        this.PasswordDao = passwordDao;
        this.PersistentTokenIssuer = persistentTokenIssuer;
        this.SecurityTokenIssuer = securityTokenIssuer;
        this.SignInModelValidator = signInModelValidator;
        this.UserDao = userDao;
    }

    private IDocumentDao<AuthenticationEvent> AuthenticationEventDao { get; }

    private IOptions<AuthenticationOptions> AuthenticationOptions { get; }

    private ICacheClient Cache { get; }

    private ICachedValueDao CachedValueDao { get; }

    private IMapper Mapper { get; }

    private IPasswordDao PasswordDao { get; }

    private IPersistentTokenIssuer PersistentTokenIssuer { get; }

    private ISecurityTokenIssuer SecurityTokenIssuer { get; }

    private IValidator<SignInModel> SignInModelValidator { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    public async Task<SignInResult> SignIn(
        AuthenticationData<SignInModel> data,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { data, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        ArgumentNullException.ThrowIfNull(data);

        var model = data.Model;

        await this.SignInModelValidator.ValidateAndThrowAsync(model, cancellationToken).ConfigureAwait(false);

        var userName = model.UserName;

        Log.ForDebugEvent()
            .Property(LogProperties.Data, new { userName })
            .Message("Fetching User document")
            .Log();

        var user = await this.UserDao.Fetch(u => u.UserName == userName, cancellationToken).ConfigureAwait(false);

        SignInResult signInResult;

        if (user != null)
        {
            Log.ForDebugEvent()
                .Property(LogProperties.Data, new { id = user.Id })
                .Message("User fetched")
                .Log();

            signInResult = await this.GetSignInResult(data, user, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            Log.ForDebugEvent()
                .Message("User not found")
                .Log();

            signInResult = new SignInResult(AuthenticationResult.NotAuthenticated);
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, RedactSignInResult(signInResult))
            .Message(LogMessages.ExitingMethod)
            .Log();

        return signInResult;
    }

    private static PersistentTokenModel RedactPersistentToken(PersistentTokenModel model)
    {
        return new PersistentTokenModel()
        {
            ClientId = model.ClientId,
            ClientToken = "REDACTED",
            ExpirationDate = model.ExpirationDate,
        };
    }

    private static SignInResult RedactSignInResult(SignInResult result)
    {
        var persistentToken = result.PersistentToken;

        return new SignInResult(
            result.AuthenticationResult,
            persistentToken != null ? RedactPersistentToken(persistentToken) : null,
            result.SessionToken);
    }

    private async Task<SignInResult> GetSignInResult(
        AuthenticationData<SignInModel> data,
        UserDocument user,
        CancellationToken cancellationToken)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { data, user, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var userStatus = user.UserStatus;
        var userId = user.Id;
        SignInResult signInResult;

        if (userStatus is not UserStatus.Suspended)
        {
            var signInAttempts = await this.Cache.IncrementCounter(userId, cancellationToken)
                .ConfigureAwait(false);

            if (signInAttempts <= this.AuthenticationOptions.Value.MaxPasswordAttempts)
            {
                // TODO: validate the user's password and force a password change if it's not valid
                signInResult = await this.GetSignInResult(data.Model, user, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                Log.ForDebugEvent()
                    .Property(LogProperties.Data, new { id = userId, userName = user.UserName })
                    .Message("User locked out due to repeated failed password attempts")
                    .Log();

                signInResult = new SignInResult(AuthenticationResult.LockedOut);
            }
        }
        else
        {
            signInResult = new SignInResult(AuthenticationResult.Suspended);
        }

        var authenticationEvent = this.Mapper.Map<AuthenticationEvent>(data);
        authenticationEvent.AuthenticationResult = signInResult.AuthenticationResult;
        authenticationEvent.UserId = userId;

        _ = await this.AuthenticationEventDao.Save(authenticationEvent, cancellationToken).ConfigureAwait(false);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, RedactSignInResult(signInResult))
            .Message(LogMessages.ExitingMethod)
            .Log();

        return signInResult;
    }

    private async Task<SignInResult> GetSignInResult(
        SignInModel model,
        UserDocument user,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { model, user, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var userId = user.Id;
        var passwordResult = await this.PasswordDao.VerifyPassword(
            userId,
            model.Password,
            cancellationToken)
            .ConfigureAwait(false);
        var authenticationResult = passwordResult is PasswordVerificationResult.VerifiedAndExpired
            || (passwordResult is PasswordVerificationResult.Verified
                && user.UserStatus is UserStatus.PasswordChangeRequired
                    or UserStatus.EmailNotVerifiedAndPasswordChangeRequired)
            ? AuthenticationResult.PasswordExpired
            : this.Mapper.Map<AuthenticationResult>(passwordResult);
        var sessionToken = default(SecurityToken);
        var persistentToken = default(PersistentTokenModel);

        if (authenticationResult is AuthenticationResult.Authenticated or AuthenticationResult.PasswordExpired)
        {
            if (passwordResult == PasswordVerificationResult.VerifiedAndExpired)
            {
                await this.CachedValueDao.SetUserStatus(userId, UserStatus.PasswordChangeRequired)
                    .ConfigureAwait(false);
            }

            sessionToken = this.SecurityTokenIssuer.Issue(userId, user);

            if (model.RememberMe && authenticationResult is not AuthenticationResult.PasswordExpired)
            {
                persistentToken = await this.PersistentTokenIssuer.Issue(userId, cancellationToken)
                    .ConfigureAwait(false);

                Log.ForDebugEvent()
                    .Property(LogProperties.Data, new { persistentToken = RedactPersistentToken(persistentToken) })
                    .Message("Persistent token issued")
                    .Log();
            }
        }
        else
        {
            Log.ForInfoEvent()
                .Property(LogProperties.Data, new { userId, authenticationResult })
                .Message("Authentication attempt failed")
                .Log();
        }

        var signInResult = new SignInResult(authenticationResult, persistentToken, sessionToken);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, RedactSignInResult(signInResult))
            .Message(LogMessages.ExitingMethod)
            .Log();

        return signInResult;
    }
}
