namespace Hirameku.Authentication;

using FluentValidation;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Microsoft.Extensions.Options;
using NLog;
using System;
using System.Globalization;
using System.Text;
using System.Threading;
using ServiceExceptions = Hirameku.Common.Service.Properties.Exceptions;

public class SendPasswordResetHandler : ISendPasswordResetHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public SendPasswordResetHandler(
        IEmailer emailer,
        IRecaptchaResponseValidator recaptchaResponseValidator,
        IValidator<SendPasswordResetModel> sendPasswordResetModelValidator,
        IDocumentDao<UserDocument> userDao,
        IVerificationDao verificationDao,
        IOptions<VerificationOptions> verificationOptions)
    {
        this.Emailer = emailer;
        this.RecaptchaResponseValidator = recaptchaResponseValidator;
        this.SendPasswordResetModelValidator = sendPasswordResetModelValidator;
        this.UserDao = userDao;
        this.VerificationDao = verificationDao;
        this.VerificationOptions = verificationOptions;
    }

    private IEmailer Emailer { get; }

    private IRecaptchaResponseValidator RecaptchaResponseValidator { get; }

    private IValidator<SendPasswordResetModel> SendPasswordResetModelValidator { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    private IVerificationDao VerificationDao { get; }

    private IOptions<VerificationOptions> VerificationOptions { get; }

    public async Task SendPasswordReset(
        SendPasswordResetModel model,
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

        var user = await this.UserDao.GetUserByUserName(model.UserName, cancellationToken).ConfigureAwait(false);

        if (user.UserStatus is UserStatus.EmailNotVerified or UserStatus.EmailNotVerifiedAndPasswordChangeRequired)
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                CompositeFormat.Parse(ServiceExceptions.EmailAddressNotVerified).Format,
                user.Id);

            throw new EmailAddressNotVerifiedException(message);
        }

        await this.SendPasssordReset(user, cancellationToken).ConfigureAwait(false);

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Log();
    }

    private async Task SendPasssordReset(UserDocument user, CancellationToken cancellationToken)
    {
        var emailAddress = user.EmailAddress;
        var verificationToken = await this.VerificationDao.GenerateVerificationToken(
            user.Id,
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
                user.UserName,
                this.VerificationOptions.Value.MaxVerificationAge),
            cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task Validate(
        SendPasswordResetModel model,
        string action,
        string remoteIP,
        CancellationToken cancellationToken)
    {
        await this.SendPasswordResetModelValidator.ValidateAndThrowAsync(model, cancellationToken)
            .ConfigureAwait(false);
        await this.RecaptchaResponseValidator.ValidateAndThrow(
            model.RecaptchaResponse,
            action,
            remoteIP,
            cancellationToken)
            .ConfigureAwait(false);
    }
}
