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

namespace Hirameku.Email;

using FluentValidation;
using Hirameku.Common;
using Hirameku.Email.Properties;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using NLog;
using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

public partial class Emailer : IEmailer
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Emailer(
        IOptions<EmailerOptions> options,
        IEmailTokenSerializer serializer,
        ISmtpClient smtpClient,
        IValidator<EmailTokenData> validator)
    {
        this.Options = options;
        this.Serializer = serializer;
        this.SmtpClient = smtpClient;
        this.Validator = validator;
    }

    private IOptions<EmailerOptions> Options { get; }

    private IEmailTokenSerializer Serializer { get; }

    private ISmtpClient SmtpClient { get; }

    private IValidator<EmailTokenData> Validator { get; }

    public async Task SendFeedbackEmail(
        string name,
        string feedback,
        string? replyToAddress = default,
        CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new
            {
                name,
                feedback,
                replyToAddress,
                cancellationToken,
            });

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(Exceptions.StringNullOrWhiteSpace, nameof(name));
        }

        if (string.IsNullOrWhiteSpace(feedback))
        {
            throw new ArgumentException(Exceptions.StringNullOrWhiteSpace, nameof(feedback));
        }

        var htmlBody = GetSendFeedbackBody("html", name, feedback, replyToAddress);
        var textBody = GetSendFeedbackBody("text", name, feedback, replyToAddress);

        using var message = this.GetMimeMessage(
            this.Options.Value.FeedbackEmailAddress,
            string.Empty,
            Resources.SendFeedbackSubject,
            htmlBody,
            textBody);

        await this.SendEmail(message, cancellationToken).ConfigureAwait(false);

        Log.Trace("Exiting method", data: default(object));
    }

    public async Task SendForgotPasswordEmail(
        string emailAddress,
        string name,
        EmailTokenData tokenData,
        CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new
            {
                emailAddress,
                name,
                tokenData = new
                {
                    pepper = "REDACTED",
                    token = tokenData?.Token,
                    userName = tokenData?.UserName,
                    validityPeriod = tokenData?.ValidityPeriod,
                },
                cancellationToken,
            });

        ArgumentNullException.ThrowIfNull(tokenData);

        await this.Validator.ValidateAndThrowAsync(tokenData, cancellationToken).ConfigureAwait(false);

        var queryStringToken = this.Serializer.Serialize(tokenData.Pepper, tokenData.Token, tokenData.UserName);
        var htmlBody = this.GetForgotPasswordBody("html", queryStringToken, tokenData.ValidityPeriod);
        var textBody = this.GetForgotPasswordBody("text", queryStringToken, tokenData.ValidityPeriod);

        using var message = this.GetMimeMessage(
            emailAddress,
            name,
            Resources.ForgotPasswordSubject,
            htmlBody,
            textBody);

        await this.SendEmail(message, cancellationToken).ConfigureAwait(false);

        Log.Trace("Exiting method", data: default(object));
    }

    public async Task SendVerificationEmail(
        string emailAddress,
        string name,
        EmailTokenData tokenData,
        CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new
            {
                emailAddress,
                name,
                tokenData = new
                {
                    pepper = "REDACTED",
                    token = tokenData?.Token,
                    userName = tokenData?.UserName,
                    validityPeriod = tokenData?.ValidityPeriod,
                },
                cancellationToken,
            });

        ArgumentNullException.ThrowIfNull(tokenData);

        await this.Validator.ValidateAndThrowAsync(tokenData, cancellationToken).ConfigureAwait(false);

        var queryStringToken = this.Serializer.Serialize(tokenData.Pepper, tokenData.Token, tokenData.UserName);
        var htmlBody = this.GetVerifyEmailBody("html", queryStringToken, tokenData.ValidityPeriod);
        var textBody = this.GetVerifyEmailBody("text", queryStringToken, tokenData.ValidityPeriod);

        using var message = this.GetMimeMessage(
            emailAddress,
            name,
            Resources.VerifyEmailSubject,
            htmlBody,
            textBody);

        await this.SendEmail(message, cancellationToken).ConfigureAwait(false);

        Log.Trace("Exiting method", data: default(object));
    }

    [GeneratedRegex(Regexes.HtmlTag)]
    private static partial Regex GeneratedHtmlTagRegex();

    private static string GetSendFeedbackBody(
        string type,
        string name,
        string feedback,
        string? replyToAddress = default)
    {
        return type switch
        {
            "html" => string.Format(
                InvariantCulture,
                CompositeFormat.Parse(Resources.SendFeedbackHtml).Format,
                CompositeFormat.Parse(Resources.SendFeedbackSubject).Format,
                replyToAddress ?? string.Empty,
                name,
                GeneratedHtmlTagRegex().IsMatch(feedback) ? WebUtility.HtmlEncode(feedback) : feedback),
            "text" => string.Format(
                InvariantCulture,
                CompositeFormat.Parse(Resources.SendFeedbackText).Format,
                name,
                replyToAddress ?? "email address not provided",
                feedback),
            _ => throw new ArgumentException(
                string.Format(
                    InvariantCulture,
                    CompositeFormat.Parse(Exceptions.UnsupportedEmailBodyType).Format,
                    type),
                nameof(type)),
        };
    }

    private static string GetValidityText(TimeSpan? validityPeriod)
    {
        var text = string.Empty;

        if (validityPeriod.HasValue)
        {
            var period = validityPeriod.Value;
            var days = period.Days;
            var hours = period.Hours;

            if (days > 0)
            {
                text = days > 1 ? days.ToString(InvariantCulture) : "1";
                text += days > 1 ? " days" : " day";
            }
            else if (hours > 0)
            {
                text = hours > 1 ? hours.ToString(InvariantCulture) : "1";
                text += hours > 1 ? " hours" : " hour";
            }
            else
            {
                var minutes = period.Minutes;

                text = minutes > 1 ? minutes.ToString(InvariantCulture) : "1";
                text += minutes > 1 ? " minutes" : " minute";
            }

            text = string.Format(InvariantCulture, CompositeFormat.Parse(Resources.ValidityPeriodText).Format, text);
        }

        return text;
    }

    private string GetForgotPasswordBody(string type, string queryStringToken, TimeSpan? validityPeriod)
    {
        var options = this.Options.Value;
        var forgotPasswordUrl = this.GetUrlWithQueryString(options.ResetPasswordUrl, queryStringToken);
        var validityText = GetValidityText(validityPeriod);

        return type switch
        {
            "html" => string.Format(
                InvariantCulture,
                CompositeFormat.Parse(Resources.ForgotPasswordHtml).Format,
                CompositeFormat.Parse(Resources.ForgotPasswordSubject).Format,
                forgotPasswordUrl,
                validityText),
            "text" => string.Format(
                InvariantCulture,
                CompositeFormat.Parse(Resources.ForgotPasswordText).Format,
                forgotPasswordUrl,
                validityText),
            _ => throw new ArgumentException(
                string.Format(
                    InvariantCulture,
                    CompositeFormat.Parse(Exceptions.UnsupportedEmailBodyType).Format,
                    type),
                nameof(type)),
        };
    }

    private MimeMessage GetMimeMessage(
        string emailAddress,
        string name,
        string subject,
        string htmlBody,
        string textBody)
    {
        var message = new MimeMessage();
        var options = this.Options.Value;
        message.From.Add(new MailboxAddress(string.Empty, options.Sender));
        message.To.Add(new MailboxAddress(name, emailAddress));
        message.Subject = subject;

        var body = new BodyBuilder()
        {
            HtmlBody = htmlBody,
            TextBody = textBody,
        };

        message.Body = body.ToMessageBody();

        return message;
    }

    private Uri GetUrlWithQueryString(Uri? baseUrl, string queryStringToken)
    {
        var options = this.Options.Value;
        const string QueryStringParameterTemplate = "{0}={1}";
        var url = new UriBuilder(baseUrl ?? new Uri(string.Empty))
        {
            Query = string.Format(
                InvariantCulture,
                QueryStringParameterTemplate,
                options.QueryStringParameterName,
                WebUtility.UrlEncode(queryStringToken)),
        };

        return url.Uri;
    }

    private string GetVerifyEmailBody(string type, string queryStringToken, TimeSpan? validityPeriod)
    {
        var options = this.Options.Value;
        var verifyEmailUrl = this.GetUrlWithQueryString(options.VerifyEmailUrl, queryStringToken);
        var validityText = GetValidityText(validityPeriod);
        var rejectRegistrationUrl = this.GetUrlWithQueryString(options.RejectRegistrationUrl, queryStringToken);

        return type switch
        {
            "html" => string.Format(
                InvariantCulture,
                CompositeFormat.Parse(Resources.VerifyEmailHtml).Format,
                CompositeFormat.Parse(Resources.VerifyEmailSubject).Format,
                verifyEmailUrl,
                validityText,
                rejectRegistrationUrl),
            "text" => string.Format(
                InvariantCulture,
                CompositeFormat.Parse(Resources.VerifyEmailText).Format,
                verifyEmailUrl,
                validityText,
                rejectRegistrationUrl),
            _ => throw new ArgumentException(
                string.Format(
                    InvariantCulture,
                    CompositeFormat.Parse(Exceptions.UnsupportedEmailBodyType).Format,
                    type),
                nameof(type)),
        };
    }

    private async Task SendEmail(MimeMessage message, CancellationToken cancellationToken)
    {
        Log.Debug("Connecting to SMTP server");

        var options = this.Options.Value;
        var client = this.SmtpClient;

        await client.ConnectAsync(options.SmtpServer, options.SmtpPort, options.UseTls, cancellationToken)
            .ConfigureAwait(false);

        var userName = options.SmtpUserName;

        if (!string.IsNullOrWhiteSpace(userName))
        {
            await client.AuthenticateAsync(userName, options.SmtpPassword, cancellationToken).ConfigureAwait(false);
        }

        Log.Debug("Sending email");

        var response = await client.SendAsync(message, cancellationToken).ConfigureAwait(false);

        Log.Info("SMTP server response", data: new { response });

        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
    }
}
