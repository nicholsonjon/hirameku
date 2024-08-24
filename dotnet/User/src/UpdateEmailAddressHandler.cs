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
using NLog;
using System.Globalization;
using System.Text;

public class UpdateEmailAddressHandler : IUpdateEmailAddressHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public UpdateEmailAddressHandler(
        ICachedValueDao cachedValueDao,
        IEmailer emailer,
        IValidator<Authenticated<UpdateEmailAddressModel>> updateEmailAddressModelValidator,
        IDocumentDao<UserDocument> userDao,
        IVerificationDao verificationDao,
        IOptions<VerificationOptions> verificationOptions)
    {
        this.CachedValueDao = cachedValueDao;
        this.Emailer = emailer;
        this.UpdateEmailAddressModelValidator = updateEmailAddressModelValidator;
        this.UserDao = userDao;
        this.VerificationDao = verificationDao;
        this.VerificationOptions = verificationOptions;
    }

    private ICachedValueDao CachedValueDao { get; }

    private IEmailer Emailer { get; }

    private IValidator<Authenticated<UpdateEmailAddressModel>> UpdateEmailAddressModelValidator { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    private IVerificationDao VerificationDao { get; }

    private IOptions<VerificationOptions> VerificationOptions { get; }

    public async Task UpdateEmailAddress(
        Authenticated<UpdateEmailAddressModel> authenticatedModel,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { authenticatedModel, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        ArgumentNullException.ThrowIfNull(authenticatedModel);

        await this.UpdateEmailAddressModelValidator.ValidateAndThrowAsync(authenticatedModel, cancellationToken)
            .ConfigureAwait(false);

        var userId = authenticatedModel.User.GetUserId()!;
        var userDao = this.UserDao;
        var user = await userDao.Fetch(userId, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                CompositeFormat.Parse(Exceptions.UserIdDoesNotExist).Format,
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
}
