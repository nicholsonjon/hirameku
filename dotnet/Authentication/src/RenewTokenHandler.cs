namespace Hirameku.Authentication;

using AutoMapper;
using FluentValidation;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Common.Service;
using Hirameku.Data;
using Microsoft.IdentityModel.Tokens;
using NLog;

public class RenewTokenHandler : IRenewTokenHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public RenewTokenHandler(
        IDocumentDao<AuthenticationEvent> authenticationEventDao,
        IMapper mapper,
        IPersistentTokenDao persistentTokenDao,
        IValidator<RenewTokenModel> renewTokenModelValidator,
        ISecurityTokenIssuer securityTokenIssuer,
        IDocumentDao<UserDocument> userDao)
    {
        this.AuthenticationEventDao = authenticationEventDao;
        this.Mapper = mapper;
        this.PersistentTokenDao = persistentTokenDao;
        this.RenewTokenModelValidator = renewTokenModelValidator;
        this.SecurityTokenIssuer = securityTokenIssuer;
        this.UserDao = userDao;
    }

    private IDocumentDao<AuthenticationEvent> AuthenticationEventDao { get; }

    private IMapper Mapper { get; }

    private IPersistentTokenDao PersistentTokenDao { get; }

    private IValidator<RenewTokenModel> RenewTokenModelValidator { get; }

    private ISecurityTokenIssuer SecurityTokenIssuer { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    public async Task<RenewTokenResult> RenewToken(
        AuthenticationData<RenewTokenModel> data,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { data, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        ArgumentNullException.ThrowIfNull(data);

        var model = data.Model;

        await this.RenewTokenModelValidator.ValidateAndThrowAsync(model, cancellationToken).ConfigureAwait(false);

        var user = await this.UserDao.GetUserById(model.UserId, cancellationToken).ConfigureAwait(false);

        RenewTokenResult renewTokenResult;

        if (user.UserStatus is not (UserStatus.PasswordChangeRequired
            or UserStatus.EmailNotVerifiedAndPasswordChangeRequired))
        {
            renewTokenResult = await this.GetRenewTokenResult(data, user, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            renewTokenResult = new RenewTokenResult(AuthenticationResult.PasswordExpired);
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, renewTokenResult)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return renewTokenResult;
    }

    private async Task<RenewTokenResult> GetRenewTokenResult(
        AuthenticationData<RenewTokenModel> data,
        UserDocument user,
        CancellationToken cancellationToken)
    {
        var userId = user.Id;
        var model = data.Model;
        var clientId = model.ClientId;
        var clientToken = model.ClientToken;
        var verificationResult = await this.PersistentTokenDao.VerifyPersistentToken(
            userId,
            clientId,
            clientToken,
            cancellationToken)
            .ConfigureAwait(false);
        var sessionToken = default(SecurityToken);

        if (verificationResult is PersistentTokenVerificationResult.Verified)
        {
            Log.ForInfoEvent()
                .Property(LogProperties.Data, new { userId, clientId, clientToken })
                .Message("Persistent token verified")
                .Log();

            sessionToken = this.SecurityTokenIssuer.Issue(userId, user);
        }
        else
        {
            Log.ForInfoEvent()
                .Property(LogProperties.Data, new { userId, clientId, clientToken, verificationResult })
                .Message("Persistent token not verified")
                .Log();
        }

        var authenticationEvent = this.Mapper.Map<AuthenticationEvent>(data);
        var authenticationResult = this.Mapper.Map<AuthenticationResult>(verificationResult);
        authenticationEvent.AuthenticationResult = authenticationResult;

        _ = await this.AuthenticationEventDao.Save(authenticationEvent, cancellationToken).ConfigureAwait(false);

        return new RenewTokenResult(authenticationResult, sessionToken);
    }
}
