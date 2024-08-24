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
using NLog;
using System.Globalization;
using System.Text;
using CommonExceptions = Hirameku.Common.Properties.Exceptions;
using UserExceptions = Hirameku.User.Properties.Exceptions;

public class ChangePasswordHandler : IChangePasswordHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ChangePasswordHandler(
        ICachedValueDao cachedValueDao,
        IValidator<Authenticated<ChangePasswordModel>> changePasswordModelValidator,
        IPasswordDao passwordDao,
        IPersistentTokenIssuer persistentTokenIssuer,
        ISecurityTokenIssuer securityTokenIssuer,
        IDocumentDao<UserDocument> userDao)
    {
        this.CachedValueDao = cachedValueDao;
        this.ChangePasswordModelValidator = changePasswordModelValidator;
        this.PasswordDao = passwordDao;
        this.PersistentTokenIssuer = persistentTokenIssuer;
        this.SecurityTokenIssuer = securityTokenIssuer;
        this.UserDao = userDao;
    }

    private ICachedValueDao CachedValueDao { get; }

    private IValidator<Authenticated<ChangePasswordModel>> ChangePasswordModelValidator { get; }

    private IPasswordDao PasswordDao { get; }

    private IPersistentTokenIssuer PersistentTokenIssuer { get; }

    private ISecurityTokenIssuer SecurityTokenIssuer { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    public async Task<TokenResponseModel> ChangePassword(
        Authenticated<ChangePasswordModel> authenticatedModel,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { authenticatedModel, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        ArgumentNullException.ThrowIfNull(authenticatedModel);

        await this.ChangePasswordModelValidator.ValidateAndThrowAsync(authenticatedModel, cancellationToken).ConfigureAwait(false);

        var userDao = this.UserDao;
        var userId = authenticatedModel.User.GetUserId()!;
        var user = await userDao.Fetch(userId, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                CompositeFormat.Parse(CommonExceptions.UserIdDoesNotExist).Format,
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
}
