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

namespace Hirameku.User;

using FluentValidation;
using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NLog;
using System.Globalization;
using CommonExceptions = Hirameku.Common.Properties.Exceptions;
using UserExceptions = Hirameku.User.Properties.Exceptions;

public class UserProvider : IUserProvider
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public UserProvider(
        ICachedValueDao cachedValueDao,
        IValidator<Authenticated<ChangePasswordModel>> changePasswordModelValidator,
        IEmailer emailer,
        IPasswordDao passwordDao,
        IPersistentTokenIssuer persistentTokenIssuer,
        ISecurityTokenIssuer securityTokenIssuer,
        IValidator<Authenticated<UpdateEmailAddressModel>> updateEmailAddressModelValidator,
        IValidator<Authenticated<UpdateNameModel>> updateNameModelValidator,
        IValidator<Authenticated<UpdateUserNameModel>> updateUserNameModelValidator,
        IDocumentDao<UserDocument> userDao,
        IVerificationDao verificationDao,
        IOptions<VerificationOptions> verificationOptions)
    {
        this.CachedValueDao = cachedValueDao;
        this.ChangePasswordModelValidator = changePasswordModelValidator;
        this.Emailer = emailer;
        this.PasswordDao = passwordDao;
        this.PersistentTokenIssuer = persistentTokenIssuer;
        this.SecurityTokenIssuer = securityTokenIssuer;
        this.UpdateEmailAddressModelValidator = updateEmailAddressModelValidator;
        this.UpdateNameModelValidator = updateNameModelValidator;
        this.UpdateUserNameModelValidator = updateUserNameModelValidator;
        this.UserDao = userDao;
        this.VerificationDao = verificationDao;
        this.VerificationOptions = verificationOptions;
    }

    private ICachedValueDao CachedValueDao { get; }

    private IValidator<Authenticated<ChangePasswordModel>> ChangePasswordModelValidator { get; }

    private IEmailer Emailer { get; }

    private IPasswordDao PasswordDao { get; }

    private IPersistentTokenIssuer PersistentTokenIssuer { get; }

    private ISecurityTokenIssuer SecurityTokenIssuer { get; }

    private IValidator<Authenticated<UpdateEmailAddressModel>> UpdateEmailAddressModelValidator { get; }

    private IValidator<Authenticated<UpdateNameModel>> UpdateNameModelValidator { get; }

    private IValidator<Authenticated<UpdateUserNameModel>> UpdateUserNameModelValidator { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    private IVerificationDao VerificationDao { get; }

    private IOptions<VerificationOptions> VerificationOptions { get; }

    public async Task<TokenResponseModel> ChangePassword(
        Authenticated<ChangePasswordModel> authenticatedModel,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { authenticatedModel, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        if (authenticatedModel is null)
        {
            throw new ArgumentNullException(nameof(authenticatedModel));
        }

        await this.ChangePasswordModelValidator.ValidateAndThrowAsync(authenticatedModel, cancellationToken).ConfigureAwait(false);

        var userDao = this.UserDao;
        var userId = authenticatedModel.User.GetUserId()!;
        var user = await userDao.Fetch(userId, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                CommonExceptions.UserIdDoesNotExist,
                userId);

            throw new UserDoesNotExistException(message);
        }

        var passwordDao = this.PasswordDao;
        var model = authenticatedModel.Model;
        var passwordResult = await passwordDao.VerifyPassword(userId, model.CurrentPassword, cancellationToken)
            .ConfigureAwait(false);

        if (passwordResult == PasswordVerificationResult.NotVerified)
        {
            throw new InvalidPasswordException(UserExceptions.InvalidPassword);
        }

        await passwordDao.SavePassword(userId, model.NewPassword, cancellationToken).ConfigureAwait(false);

        var userStatus = user.UserStatus;

        if (userStatus == UserStatus.PasswordChangeRequired)
        {
            await this.CachedValueDao.SetUserStatus(userId, UserStatus.OK).ConfigureAwait(false);
        }
        else if (userStatus == UserStatus.EmailNotVerifiedAndPasswordChangeRequired)
        {
            await this.CachedValueDao.SetUserStatus(userId, UserStatus.EmailNotVerified).ConfigureAwait(false);
        }

        var sessionToken = this.SecurityTokenIssuer.Issue(userId, user);
        var persistentToken = default(PersistentTokenModel);

        if (model.RememberMe)
        {
            persistentToken = await this.PersistentTokenIssuer.Issue(userId, cancellationToken)
                .ConfigureAwait(false);
        }

        var response = new TokenResponseModel(sessionToken, persistentToken);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, response)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return response;
    }

    public async Task DeleteUser(
        Authenticated<Unit> authenticatedModel,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { authenticatedModel, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        if (authenticatedModel is null)
        {
            throw new ArgumentNullException(nameof(authenticatedModel));
        }

        var userId = authenticatedModel.User.GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                UserExceptions.MissingClaim,
                userId);

            throw new ArgumentException(message, nameof(authenticatedModel));
        }

        // TODO: broadcast user deletion event message
        await this.UserDao.Delete(userId, cancellationToken).ConfigureAwait(false);

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Log();
    }

    public async Task<User?> GetUser(
        Authenticated<Unit> authenticatedModel,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { authenticatedModel, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        if (authenticatedModel is null)
        {
            throw new ArgumentNullException(nameof(authenticatedModel));
        }

        var userId = authenticatedModel.User.GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                UserExceptions.MissingClaim,
                userId);

            throw new ArgumentException(message, nameof(authenticatedModel));
        }

        var user = await this.UserDao.Fetch(userId, cancellationToken).ConfigureAwait(false);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, user)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return user;
    }

    public async Task UpdateEmailAddress(
        Authenticated<UpdateEmailAddressModel> authenticatedModel,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { authenticatedModel, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        if (authenticatedModel is null)
        {
            throw new ArgumentNullException(nameof(authenticatedModel));
        }

        await this.UpdateEmailAddressModelValidator.ValidateAndThrowAsync(authenticatedModel, cancellationToken)
            .ConfigureAwait(false);

        var userId = authenticatedModel.User.GetUserId()!;
        var userDao = this.UserDao;
        var user = await userDao.Fetch(userId, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                Exceptions.UserIdDoesNotExist,
                userId);

            throw new UserDoesNotExistException(message);
        }

        var emailAddress = authenticatedModel.Model.EmailAddress;

        await userDao.Update(userId, u => u.EmailAddress, emailAddress, cancellationToken).ConfigureAwait(false);

        var newStatus = user.UserStatus == UserStatus.PasswordChangeRequired
            ? UserStatus.EmailNotVerifiedAndPasswordChangeRequired
            : UserStatus.EmailNotVerified;

        await this.CachedValueDao.SetUserStatus(userId, newStatus).ConfigureAwait(false);

        // disallow cancellation after the email has been updated to ensure the user receives the verification link
        // to update their UserStatus
        var noCancellation = CancellationToken.None;
        var verificationToken = await this.VerificationDao.GenerateVerificationToken(
            userId,
            emailAddress,
            VerificationType.EmailVerification,
            noCancellation)
            .ConfigureAwait(false);

        await this.Emailer.SendVerificationEmail(
            emailAddress,
            user.Name,
            new EmailTokenData(
                verificationToken.Pepper,
                verificationToken.Token,
                user.UserName,
                this.VerificationOptions.Value.MaxVerificationAge),
            noCancellation)
            .ConfigureAwait(false);

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Log();
    }

    public async Task<SecurityToken> UpdateName(
        Authenticated<UpdateNameModel> authenticatedModel,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { authenticatedModel, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        if (authenticatedModel is null)
        {
            throw new ArgumentNullException(nameof(authenticatedModel));
        }

        await this.UpdateNameModelValidator.ValidateAndThrowAsync(authenticatedModel, cancellationToken)
            .ConfigureAwait(false);

        var userId = authenticatedModel.User.GetUserId()!;
        var userDao = this.UserDao;
        var user = await userDao.Fetch(userId, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                CommonExceptions.UserIdDoesNotExist,
                userId);

            throw new UserDoesNotExistException(message);
        }

        await userDao.Update(
            userId,
            u => u.Name,
            authenticatedModel.Model.Name,
            cancellationToken)
            .ConfigureAwait(false);

        var sessionToken = this.SecurityTokenIssuer.Issue(userId, user, authenticatedModel.SecurityToken.ValidTo);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, sessionToken)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return sessionToken;
    }

    public async Task<SecurityToken> UpdateUserName(
        Authenticated<UpdateUserNameModel> authenticatedModel,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { authenticatedModel, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        if (authenticatedModel is null)
        {
            throw new ArgumentNullException(nameof(authenticatedModel));
        }

        await this.UpdateUserNameModelValidator.ValidateAndThrowAsync(authenticatedModel, cancellationToken)
            .ConfigureAwait(false);

        var userId = authenticatedModel.User.GetUserId()!;
        var userDao = this.UserDao;
        var user = await userDao.Fetch(userId, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                CommonExceptions.UserIdDoesNotExist,
                userId);

            throw new UserDoesNotExistException(message);
        }

        await userDao.Update(
            userId,
            d => d.UserName,
            authenticatedModel.Model.UserName,
            cancellationToken)
            .ConfigureAwait(false);

        var sessionToken = this.SecurityTokenIssuer.Issue(userId, user, authenticatedModel.SecurityToken.ValidTo);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, sessionToken)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return sessionToken;
    }
}
