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
using NLog;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EmailExceptions = Hirameku.Email.Properties.Exceptions;
using RegistrationExceptions = Hirameku.Registration.Properties.Exceptions;

public partial class RegistrationProvider : IRegistrationProvider
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public RegistrationProvider(
        ICacheClient cache,
        ICachedValueDao cachedValueDao,
        IEmailer emailer,
        IEmailTokenSerializer emailTokenSerializer,
        IMapper mapper,
        IPasswordDao passwordDao,
        IPasswordValidator passwordValidator,
        IRecaptchaResponseValidator recaptchaResponseValidator,
        IValidator<RegisterModel> registerModelValidator,
        IValidator<ResendVerificationEmailModel> resendVerificationEmailModel,
        IDocumentDao<UserDocument> userDao,
        IVerificationDao verificationDao,
        IOptions<VerificationOptions> verificationOptions)
    {
        this.Cache = cache;
        this.CachedValueDao = cachedValueDao;
        this.Emailer = emailer;
        this.EmailTokenSerializer = emailTokenSerializer;
        this.Mapper = mapper;
        this.PasswordDao = passwordDao;
        this.PasswordValidator = passwordValidator;
        this.RecaptchaResponseValidator = recaptchaResponseValidator;
        this.RegisterModelValidator = registerModelValidator;
        this.ResendVerificationEmailModelValidator = resendVerificationEmailModel;
        this.UserDao = userDao;
        this.VerificationDao = verificationDao;
        this.VerificationOptions = verificationOptions;
    }

    private ICacheClient Cache { get; }

    private ICachedValueDao CachedValueDao { get; }

    private IEmailer Emailer { get; }

    private IEmailTokenSerializer EmailTokenSerializer { get; }

    private IMapper Mapper { get; }

    private IPasswordDao PasswordDao { get; }

    private IPasswordValidator PasswordValidator { get; }

    private IRecaptchaResponseValidator RecaptchaResponseValidator { get; }

    private IValidator<RegisterModel> RegisterModelValidator { get; }

    private IValidator<ResendVerificationEmailModel> ResendVerificationEmailModelValidator { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    private IVerificationDao VerificationDao { get; }

    private IOptions<VerificationOptions> VerificationOptions { get; }

    public async Task<bool> IsUserNameAvailable(string userName, CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { userName, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var isUserNameAvailable = GeneratedUserNameRegex().IsMatch(userName);

        if (isUserNameAvailable)
        {
            var count = await this.UserDao.GetCount(u => u.UserName == userName, cancellationToken)
                .ConfigureAwait(false);

            isUserNameAvailable = count == 0;
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, isUserNameAvailable)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return isUserNameAvailable;
    }

    public async Task Register(
        RegisterModel model,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { model, action, remoteIP, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        ArgumentNullException.ThrowIfNull(model);

        await this.RegisterModelValidator.ValidateAndThrowAsync(model, cancellationToken).ConfigureAwait(false);
        await this.RecaptchaResponseValidator.ValidateAndThrow(
            model.RecaptchaResponse,
            action,
            remoteIP,
            cancellationToken)
            .ConfigureAwait(false);
        await this.ValidateIsNotDuplicateRegistration(model, cancellationToken).ConfigureAwait(false);

        var saveResult = await this.UserDao.Save(this.Mapper.Map<UserDocument>(model), cancellationToken)
            .ConfigureAwait(false);
        var userId = saveResult.Id;
        var userName = model.UserName;

        Log.ForDebugEvent()
            .Property(LogProperties.Data, new { userId, userName })
            .Message("New User created")
            .Log();

        // we don't want to leave the user in a partially created state due to a cancellation, so we switch to
        // CancellationToken.None here to ignore cancellations beyond this point
        var noCancellation = CancellationToken.None;
        await this.PasswordDao.SavePassword(userId, model.Password, noCancellation).ConfigureAwait(false);

        var token = await this.VerificationDao.GenerateVerificationToken(
            userId,
            model.EmailAddress,
            VerificationType.EmailVerification,
            noCancellation)
            .ConfigureAwait(false);

        Log.ForDebugEvent()
            .Property(LogProperties.Data, new { token.EmailAddress, token.ExpirationDate, token.Token })
            .Message("Generated email verification token")
            .Log();

        var emailAddress = model.EmailAddress;

        await this.Emailer.SendVerificationEmail(
            emailAddress,
            model.Name,
            new EmailTokenData(
                token.Pepper,
                token.Token,
                model.UserName,
                this.VerificationOptions.Value.MaxVerificationAge),
            noCancellation)
            .ConfigureAwait(false);

        // seed the cache with the newly registered email address to prevent the user from immediately requesting
        // the verification email to be resent
        _ = await this.Cache.GetCooldownStatus(emailAddress, noCancellation).ConfigureAwait(false);

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Log();
    }

    public async Task RejectRegistration(string serializedToken, CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { serializedToken = "REDACTED", cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var (pepper, token, userName) = this.EmailTokenSerializer.Deserialize(serializedToken);
        var user = await this.UserDao.GetUserByUserName(userName, cancellationToken).ConfigureAwait(false);
        var userId = user.Id;
        var result = await this.VerificationDao.VerifyToken(
            userId,
            user.EmailAddress,
            VerificationType.EmailVerification,
            token,
            pepper,
            cancellationToken)
            .ConfigureAwait(false);

        if (result is VerificationTokenVerificationResult.Verified)
        {
            Log.ForInfoEvent()
                .Property(LogProperties.Data, new { userId })
                .Message("Registration rejected. User will be suspended.")
                .Log();

            await this.CachedValueDao.SetUserStatus(userId, UserStatus.Suspended).ConfigureAwait(false);
        }
        else
        {
            throw new InvalidTokenException(EmailExceptions.InvalidToken);
        }

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Log();
    }

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

        await this.ResendVerificationEmailModelValidator.ValidateAndThrowAsync(model, cancellationToken)
            .ConfigureAwait(false);
        await this.RecaptchaResponseValidator.ValidateAndThrow(
            model.RecaptchaResponse,
            action,
            remoteIP,
            cancellationToken)
            .ConfigureAwait(false);

        var emailAddress = model.EmailAddress;
        var user = await this.UserDao.GetUserByEmail(emailAddress, cancellationToken).ConfigureAwait(false);

        if (user.UserStatus is UserStatus.OK or UserStatus.PasswordChangeRequired)
        {
            throw new EmailAddressAlreadyVerifiedException(RegistrationExceptions.EmailAddressAlreadyVerified);
        }

        var cooldown = await this.Cache.GetCooldownStatus(emailAddress, cancellationToken)
            .ConfigureAwait(false);
        var resend = !cooldown.IsOnCooldown;

        if (resend)
        {
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

        var result = new ResendVerificationEmailResult(cooldown.TimeToLive, cooldown.ExpireTime, resend);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, result)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }

    public async Task<PasswordValidationResult> ValidatePassword(
        string password,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { password = "REDACTED", cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var result = await this.PasswordValidator.Validate(password, cancellationToken)
            .ConfigureAwait(false);
        var mappedResult = this.Mapper.Map<PasswordValidationResult>(result);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, mappedResult)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return mappedResult;
    }

    public async Task<EmailVerificationResult> VerifyEmaiAddress(
        string serializedToken,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { serializedToken = "REDACTED", cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var (pepper, token, userName) = this.EmailTokenSerializer.Deserialize(serializedToken);
        var user = await this.UserDao.GetUserByUserName(userName, cancellationToken).ConfigureAwait(false);
        var tokenResult = await this.VerificationDao.VerifyToken(
            user.Id,
            user.EmailAddress,
            VerificationType.EmailVerification,
            token,
            pepper,
            cancellationToken)
            .ConfigureAwait(false);
        var emailResult = this.Mapper.Map<EmailVerificationResult>(tokenResult);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, emailResult)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return emailResult;
    }

    [GeneratedRegex(Regexes.UserName)]
    private static partial Regex GeneratedUserNameRegex();

    private async Task ValidateIsNotDuplicateRegistration(RegisterModel model, CancellationToken cancellationToken)
    {
        var userName = model.UserName;
        var emailAddress = model.EmailAddress;

        Log.ForDebugEvent()
            .Property(LogProperties.Data, new { userName, emailAddress })
            .Message("Fetching user")
            .Log();

        var count = await this.UserDao.GetCount(
            u => u.UserName == userName || u.EmailAddress == emailAddress,
            cancellationToken)
            .ConfigureAwait(false);

        if (count > 0)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                CompositeFormat.Parse(RegistrationExceptions.DuplicateRegistration).Format,
                userName,
                emailAddress);

            throw new UserAlreadyExistsException(message);
        }

        Log.ForDebugEvent()
            .Message("No duplicate registration found")
            .Log();
    }
}
