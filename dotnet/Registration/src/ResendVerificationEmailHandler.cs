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

namespace Hirameku.Registration;

using FluentValidation;
using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Microsoft.Extensions.Options;
using NLog;
using System;
using System.Threading;
using RegistrationExceptions = Hirameku.Registration.Properties.Exceptions;

public class ResendVerificationEmailHandler : IResendVerificationHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ResendVerificationEmailHandler(
        ICacheClient cache,
        IEmailer emailer,
        IRecaptchaResponseValidator recaptchaResponseValidator,
        IValidator<ResendVerificationEmailModel> resendVerificationEmailModel,
        IDocumentDao<UserDocument> userDao,
        IVerificationDao verificationDao,
        IOptions<VerificationOptions> verificationOptions)
    {
        this.Cache = cache;
        this.Emailer = emailer;
        this.RecaptchaResponseValidator = recaptchaResponseValidator;
        this.ResendVerificationEmailModelValidator = resendVerificationEmailModel;
        this.UserDao = userDao;
        this.VerificationDao = verificationDao;
        this.VerificationOptions = verificationOptions;
    }

    private ICacheClient Cache { get; }

    private IEmailer Emailer { get; }

    private IRecaptchaResponseValidator RecaptchaResponseValidator { get; }

    private IValidator<ResendVerificationEmailModel> ResendVerificationEmailModelValidator { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    private IVerificationDao VerificationDao { get; }

    private IOptions<VerificationOptions> VerificationOptions { get; }

    public async Task<ResendVerificationEmailResult> ResendVerificationEmail(
        ResendVerificationEmailModel model,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { model, action, remoteIP, cancellationToken, })
            .Message(LogMessages.EnteringMethod)
            .Log();

        ArgumentNullException.ThrowIfNull(model);

        await this.Validate(model, action, remoteIP, cancellationToken).ConfigureAwait(false);

        var user = await this.UserDao.GetUserByEmail(model.EmailAddress, cancellationToken).ConfigureAwait(false);

        if (user.UserStatus is UserStatus.OK or UserStatus.PasswordChangeRequired)
        {
            throw new EmailAddressAlreadyVerifiedException(RegistrationExceptions.EmailAddressAlreadyVerified);
        }

        var result = await this.ResendVerificationEmail(user, cancellationToken).ConfigureAwait(false);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, result)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }

    private async Task<ResendVerificationEmailResult> ResendVerificationEmail(
        UserDocument user,
        CancellationToken cancellationToken)
    {
        var emailAddress = user.EmailAddress;
        var cooldown = await this.Cache.GetCooldownStatus(emailAddress, cancellationToken)
            .ConfigureAwait(false);
        var resend = !cooldown.IsOnCooldown;

        if (resend)
        {
            Log.ForDebugEvent()
                .Property(LogProperties.Data, new { emailAddress, cooldown })
                .Message("Cooldown has expired. Resending the verification email.")
                .Log();

            var verificationToken = await this.VerificationDao.GenerateVerificationToken(
                user.Id,
                emailAddress,
                VerificationType.EmailVerification,
                cancellationToken)
                .ConfigureAwait(false);

            await this.Emailer.SendVerificationEmail(
                emailAddress,
                user.Name,
                new EmailTokenData(
                    verificationToken.Pepper,
                    verificationToken.Token,
                    user.UserName,
                    this.VerificationOptions.Value.MaxVerificationAge),
                cancellationToken)
                .ConfigureAwait(false);
        }

        return new ResendVerificationEmailResult(cooldown.TimeToLive, cooldown.ExpireTime, resend);
    }

    private async Task Validate(
        ResendVerificationEmailModel model,
        string action,
        string remoteIP,
        CancellationToken cancellationToken)
    {
        await this.ResendVerificationEmailModelValidator.ValidateAndThrowAsync(model, cancellationToken)
            .ConfigureAwait(false);
        await this.RecaptchaResponseValidator.ValidateAndThrow(
            model.RecaptchaResponse,
            action,
            remoteIP,
            cancellationToken)
            .ConfigureAwait(false);
    }
}
