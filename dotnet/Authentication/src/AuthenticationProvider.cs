// Hirameku is a cloud-native, vendor-agnostic, serverless application for
// studying flashcards with support for localization and accessibility.
// Copyright (C) 2023 Jon Nicholson
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

namespace Hirameku.Authentication;

using AutoMapper;
using FluentValidation;
using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NLog;
using System.Globalization;
using ServiceExceptions = Hirameku.Common.Service.Properties.Exceptions;

public class AuthenticationProvider : IAuthenticationProvider
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public AuthenticationProvider(
        IDocumentDao<AuthenticationEvent> authenticationEventDao,
        IOptions<AuthenticationOptions> authenticationOptions,
        ICacheClient cache,
        ICachedValueDao cachedValueDao,
        IEmailer emailer,
        IEmailTokenSerializer emailTokenSerializer,
        IMapper mapper,
        IPasswordDao passwordDao,
        IPersistentTokenDao persistentTokenDao,
        IPersistentTokenIssuer persistentTokenIssuer,
        IRecaptchaResponseValidator recaptchaResponseValidator,
        IValidator<RenewTokenModel> renewTokenModelValidator,
        IValidator<ResetPasswordModel> resetPasswordModelValidator,
        ISecurityTokenIssuer securityTokenIssuer,
        IValidator<SendPasswordResetModel> sendPasswordResetModelValidator,
        IValidator<SignInModel> signInModelValidator,
        IDocumentDao<UserDocument> userDao,
        IVerificationDao verificationDao,
        IOptions<VerificationOptions> verificationOptions)
    {
        this.AuthenticationEventDao = authenticationEventDao;
        this.AuthenticationOptions = authenticationOptions;
        this.Cache = cache;
        this.CachedValueDao = cachedValueDao;
        this.Emailer = emailer;
        this.EmailTokenSerializer = emailTokenSerializer;
        this.Mapper = mapper;
        this.PasswordDao = passwordDao;
        this.PersistentTokenDao = persistentTokenDao;
        this.PersistentTokenIssuer = persistentTokenIssuer;
        this.RecaptchaResponseValidator = recaptchaResponseValidator;
        this.RenewTokenModelValidator = renewTokenModelValidator;
        this.ResetPasswordModelValidator = resetPasswordModelValidator;
        this.SecurityTokenIssuer = securityTokenIssuer;
        this.SendPasswordResetModelValidator = sendPasswordResetModelValidator;
        this.SignInModelValidator = signInModelValidator;
        this.UserDao = userDao;
        this.VerificationDao = verificationDao;
        this.VerificationOptions = verificationOptions;
    }

    private IDocumentDao<AuthenticationEvent> AuthenticationEventDao { get; }

    private IOptions<AuthenticationOptions> AuthenticationOptions { get; }

    private ICacheClient Cache { get; }

    private ICachedValueDao CachedValueDao { get; }

    private IEmailer Emailer { get; }

    private IEmailTokenSerializer EmailTokenSerializer { get; }

    private IMapper Mapper { get; }

    private IPasswordDao PasswordDao { get; }

    private IPersistentTokenDao PersistentTokenDao { get; }

    private IPersistentTokenIssuer PersistentTokenIssuer { get; }

    private IRecaptchaResponseValidator RecaptchaResponseValidator { get; }

    private IValidator<RenewTokenModel> RenewTokenModelValidator { get; }

    private IValidator<ResetPasswordModel> ResetPasswordModelValidator { get; }

    private ISecurityTokenIssuer SecurityTokenIssuer { get; }

    private IValidator<SendPasswordResetModel> SendPasswordResetModelValidator { get; }

    private IValidator<SignInModel> SignInModelValidator { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    private IVerificationDao VerificationDao { get; }

    private IOptions<VerificationOptions> VerificationOptions { get; }

    public async Task<SignInResult> SignIn(
        AuthenticationData<SignInModel> data,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { data, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

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

    public async Task<RenewTokenResult> RenewToken(
        AuthenticationData<RenewTokenModel> data,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { data, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

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

    public async Task<ResetPasswordResult> ResetPassword(
        ResetPasswordModel model,
        string hostname,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { model, hostname, action, remoteIP, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        await this.ResetPasswordModelValidator.ValidateAndThrowAsync(model, cancellationToken)
            .ConfigureAwait(false);
        await this.RecaptchaResponseValidator.ValidateAndThrow(
            model.RecaptchaResponse,
            hostname,
            action,
            remoteIP,
            cancellationToken)
            .ConfigureAwait(false);

        var (pepper, token, userName) = this.EmailTokenSerializer.Deserialize(model.SerializedToken);
        var user = await this.UserDao.GetUserByUserName(userName, cancellationToken).ConfigureAwait(false);
        var userId = user.Id;
        var verificationResult = await this.VerificationDao.VerifyToken(
            userId,
            user.EmailAddress,
            VerificationType.PasswordReset,
            token,
            pepper,
            cancellationToken)
            .ConfigureAwait(false);

        if (verificationResult == VerificationTokenVerificationResult.Verified)
        {
            await this.PasswordDao.SavePassword(userId, model.Password, cancellationToken).ConfigureAwait(false);

            if (user.UserStatus == UserStatus.PasswordChangeRequired)
            {
                await this.CachedValueDao.SetUserStatus(userId, UserStatus.OK).ConfigureAwait(false);
            }
            else if (user.UserStatus == UserStatus.EmailNotVerifiedAndPasswordChangeRequired)
            {
                await this.CachedValueDao.SetUserStatus(userId, UserStatus.EmailNotVerified).ConfigureAwait(false);
            }
        }

        var resetResult = this.Mapper.Map<ResetPasswordResult>(verificationResult);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, resetResult)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return resetResult;
    }

    public async Task SendPasswordReset(
        SendPasswordResetModel model,
        string hostname,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { model, hostname, action, remoteIP, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        await this.SendPasswordResetModelValidator.ValidateAndThrowAsync(model, cancellationToken)
            .ConfigureAwait(false);
        await this.RecaptchaResponseValidator.ValidateAndThrow(
            model.RecaptchaResponse,
            hostname,
            action,
            remoteIP,
            cancellationToken)
            .ConfigureAwait(false);

        var userName = model.UserName;
        var user = await this.UserDao.GetUserByUserName(userName, cancellationToken).ConfigureAwait(false);
        var userId = user.Id;

        if (user.UserStatus is UserStatus.EmailNotVerified or UserStatus.EmailNotVerifiedAndPasswordChangeRequired)
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                ServiceExceptions.EmailAddressNotVerified,
                userId);

            throw new EmailAddressNotVerifiedException(message);
        }

        var emailAddress = user.EmailAddress;
        var verificationToken = await this.VerificationDao.GenerateVerificationToken(
            userId,
            emailAddress,
            VerificationType.PasswordReset,
            cancellationToken)
            .ConfigureAwait(false);

        await this.Emailer.SendVerificationEmail(
            emailAddress,
            user.Name,
            new EmailTokenData(
                verificationToken.Pepper,
                verificationToken.Token,
                userName,
                this.VerificationOptions.Value.MaxVerificationAge),
            cancellationToken)
            .ConfigureAwait(false);

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Log();
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

    private static object RedactSignInResult(SignInResult result)
    {
        var persistentToken = result.PersistentToken;

        return new SignInResult(
            result.AuthenticationResult,
            persistentToken != null ? RedactPersistentToken(persistentToken) : null,
            result.SessionToken);
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
