namespace Hirameku.Contact;

using FluentValidation;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Email;
using Hirameku.Recaptcha;
using NLog;
using System;
using System.Threading.Tasks;

public class SendFeedbackHandler : ISendFeedbackHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public SendFeedbackHandler(
        IEmailer emailer,
        IRecaptchaResponseValidator recaptchaResponseValidator,
        IValidator<SendFeedbackModel> sendFeedbackModelValidator)
    {
        this.Emailer = emailer ?? throw new ArgumentNullException(nameof(emailer));
        this.RecaptchaResponseValidator = recaptchaResponseValidator
            ?? throw new ArgumentNullException(nameof(recaptchaResponseValidator));
        this.SendFeedbackModelValidator = sendFeedbackModelValidator
            ?? throw new ArgumentNullException(nameof(sendFeedbackModelValidator));
    }

    private IEmailer Emailer { get; }

    private IRecaptchaResponseValidator RecaptchaResponseValidator { get; }

    private IValidator<SendFeedbackModel> SendFeedbackModelValidator { get; }

    public async Task SendFeedback(
        SendFeedbackModel model,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { model, action, remoteIP, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        ArgumentNullException.ThrowIfNull(model);

        await this.SendFeedbackModelValidator.ValidateAndThrowAsync(model, cancellationToken).ConfigureAwait(false);
        await this.RecaptchaResponseValidator.ValidateAndThrow(
            model.RecaptchaResponse,
            action,
            remoteIP,
            cancellationToken)
            .ConfigureAwait(false);
        await this.Emailer.SendFeedbackEmail(model.Name, model.Feedback, model.EmailAddress, cancellationToken)
            .ConfigureAwait(false);

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Log();
    }
}
