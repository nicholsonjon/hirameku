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
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Microsoft.Extensions.Options;
using NLog;
using System;
using System.Globalization;
using System.Text;
using System.Threading;
using RegistrationExceptions = Hirameku.Registration.Properties.Exceptions;

public class RegisterHandler : IRegisterHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public RegisterHandler(
        ICacheClient cache,
        IEmailer emailer,
        IMapper mapper,
        IPasswordDao passwordDao,
        IRecaptchaResponseValidator recaptchaResponseValidator,
        IValidator<RegisterModel> registerModelValidator,
        IDocumentDao<UserDocument> userDao,
        IVerificationDao verificationDao,
        IOptions<VerificationOptions> verificationOptions)
    {
        this.Cache = cache;
        this.Emailer = emailer;
        this.Mapper = mapper;
        this.PasswordDao = passwordDao;
        this.RecaptchaResponseValidator = recaptchaResponseValidator;
        this.RegisterModelValidator = registerModelValidator;
        this.UserDao = userDao;
        this.VerificationDao = verificationDao;
        this.VerificationOptions = verificationOptions;
    }

    private ICacheClient Cache { get; }

    private IEmailer Emailer { get; }

    private IMapper Mapper { get; }

    private IPasswordDao PasswordDao { get; }

    private IRecaptchaResponseValidator RecaptchaResponseValidator { get; }

    private IValidator<RegisterModel> RegisterModelValidator { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    private IVerificationDao VerificationDao { get; }

    private IOptions<VerificationOptions> VerificationOptions { get; }

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

        await this.Validate(model, action, remoteIP, cancellationToken).ConfigureAwait(false);

        var userId = await this.SaveUser(model, cancellationToken).ConfigureAwait(false);

        // we don't want to leave the user in a partially created state due to cancellation, so we switch to
        // CancellationToken.None here to ignore cancellations beyond this point
        var noCancellation = CancellationToken.None;
        await this.PasswordDao.SavePassword(userId, model.Password, noCancellation).ConfigureAwait(false);

        var emailAddress = model.EmailAddress;

        var verificationToken = await this.GenerateVerificationToken(userId, emailAddress, noCancellation)
            .ConfigureAwait(false);

        await this.SendVerificationToken(model, verificationToken, noCancellation).ConfigureAwait(false);

        // seed the cache with the newly registered email address to prevent the user from immediately requesting
        // the verification email to be resent
        _ = await this.Cache.GetCooldownStatus(emailAddress, noCancellation).ConfigureAwait(false);

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Log();
    }

    private async Task<VerificationToken> GenerateVerificationToken(
        string userId,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        var verificationToken = await this.VerificationDao.GenerateVerificationToken(
            userId,
            emailAddress,
            VerificationType.EmailVerification,
            cancellationToken)
            .ConfigureAwait(false);

        Log.ForDebugEvent()
            .Property(
                LogProperties.Data,
                new { verificationToken.EmailAddress, verificationToken.ExpirationDate, verificationToken.Token })
            .Message("Generated email verification token")
            .Log();

        return verificationToken;
    }

    private async Task<string> SaveUser(RegisterModel model, CancellationToken cancellationToken)
    {
        var saveResult = await this.UserDao.Save(this.Mapper.Map<UserDocument>(model), cancellationToken)
            .ConfigureAwait(false);
        var userId = saveResult.Id;

        Log.ForDebugEvent()
            .Property(LogProperties.Data, new { userId, userName = model.UserName })
            .Message("New User created")
            .Log();

        return userId;
    }

    private async Task SendVerificationToken(
        RegisterModel model,
        VerificationToken verificationToken,
        CancellationToken cancellationToken)
    {
        await this.Emailer.SendVerificationEmail(
            model.EmailAddress,
            model.Name,
            new EmailTokenData(
                verificationToken.Pepper,
                verificationToken.Token,
                model.UserName,
                this.VerificationOptions.Value.MaxVerificationAge),
            cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task Validate(
        RegisterModel model,
        string action,
        string remoteIP,
        CancellationToken cancellationToken)
    {
        await this.RegisterModelValidator.ValidateAndThrowAsync(model, cancellationToken).ConfigureAwait(false);
        await this.RecaptchaResponseValidator.ValidateAndThrow(
            model.RecaptchaResponse,
            action,
            remoteIP,
            cancellationToken)
            .ConfigureAwait(false);

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
