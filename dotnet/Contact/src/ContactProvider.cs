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

namespace Hirameku.Contact;

using FluentValidation;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Email;
using Hirameku.Recaptcha;
using NLog;

public class ContactProvider : IContactProvider
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ContactProvider(
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
        string hostname,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { model })
            .Message(LogMessages.EnteringMethod)
            .Log();

        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        await this.SendFeedbackModelValidator.ValidateAndThrowAsync(model, cancellationToken).ConfigureAwait(false);
        await this.RecaptchaResponseValidator.ValidateAndThrow(
            model.RecaptchaResponse,
            hostname,
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
