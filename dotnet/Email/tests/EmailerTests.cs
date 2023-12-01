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

namespace Hirameku.Email.Tests;

using FluentValidation;
using Hirameku.Email.Properties;
using Hirameku.TestTools;
using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;
using System.Globalization;
using System.Text;
using System.Threading;

[TestClass]
public class EmailerTests
{
    private const string EmailAddress = "recipient@localhost";
    private const string Feedback = nameof(Feedback);
    private const string Name = nameof(Name);
    private const string Pepper = TestData.Pepper;
    private const string QueryStringParameterName = nameof(QueryStringParameterName);
    private const string Sender = "sender@localhost";
    private const string SerializedToken = nameof(SerializedToken);
    private const string SmtpPassword = nameof(SmtpPassword);
    private const int SmtpPort = 25;
    private const string SmtpServer = nameof(SmtpServer);
    private const string SmtpUserName = nameof(SmtpUserName);
    private const string Token = TestData.Token;
    private const bool UseTls = true;
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
    private static readonly Uri RejectRegistrationUrl =
        new("http://localhost/rejectregistration?" + QueryStringParameterName + "=" + SerializedToken);

    private static readonly Uri ResetPasswordUrl =
        new("http://localhost/resetpassword?" + QueryStringParameterName + "=" + SerializedToken);

    private static readonly Uri VerifyEmailUrl =
        new("http://localhost/verifyemail?" + QueryStringParameterName + "=" + SerializedToken);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Emailer_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task Emailer_SendFeedbackEmail()
    {
        await RunAndAssertSendFeedbackEmailTest().ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task Emailer_SendFeedbackEmail_SmtpServerUnauthenticated()
    {
        await RunAndAssertSendFeedbackEmailTest(useCredentials: false).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(Emailer_SendFeedbackEmail_FeedbackIsNullOrWhiteSpace_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(Emailer_SendFeedbackEmail_FeedbackIsNullOrWhiteSpace_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(Emailer_SendFeedbackEmail_FeedbackIsNullOrWhiteSpace_Throws) + "(WhiteSpace)")]
    public async Task Emailer_SendFeedbackEmail_FeedbackIsNullOrWhiteSpace_Throws(string feedback)
    {
        var target = GetTarget();

        await target.SendFeedbackEmail(Name, feedback, EmailAddress).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(Emailer_SendFeedbackEmail_NameIsNullOrWhiteSpace_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(Emailer_SendFeedbackEmail_NameIsNullOrWhiteSpace_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(Emailer_SendFeedbackEmail_NameIsNullOrWhiteSpace_Throws) + "(WhiteSpace)")]
    public async Task Emailer_SendFeedbackEmail_NameIsNullOrWhiteSpace_Throws(string name)
    {
        var target = GetTarget();

        await target.SendFeedbackEmail(name, Feedback, EmailAddress).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Emailer_SendForgotPasswordEmail(bool doesLinkExpire)
    {
        await RunAndAssertSendForgotPasswordEmailTest(doesLinkExpire).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task Emailer_SendForgotPasswordEmail_SmtpServerUnauthenticated()
    {
        await RunAndAssertSendForgotPasswordEmailTest(useCredentials: false).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task Emailer_SendForgotPasswordEmail_TokenDataIsInvalid_Throws()
    {
        var target = GetTarget();

        await target.SendForgotPasswordEmail(
            EmailAddress,
            Name,
            new EmailTokenData(null!, null!, null!, default))
            .ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task Emailer_SendForgotPasswordEmail_TokenDataIsNull_Throws()
    {
        var target = GetTarget();

        await target.SendForgotPasswordEmail(EmailAddress, Name, null!).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Emailer_SendVerificationEmail(bool doesLinkExpire)
    {
        await RunAndAssertSendVerificationEmailTest(doesLinkExpire).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task Emailer_SendVerificationEmail_SmtpServerUnauthenticated()
    {
        await RunAndAssertSendVerificationEmailTest(useCredentials: true).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task Emailer_SendVerificationEmail_TokenDataIsInvalid_Throws()
    {
        var target = GetTarget();

        await target.SendVerificationEmail(
            EmailAddress,
            Name,
            new EmailTokenData(null!, null!, null!, default))
            .ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task Emailer_SendVerificationEmail_TokenDataIsNull_Throws()
    {
        var target = GetTarget();

        await target.SendVerificationEmail(EmailAddress, Name, null!).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    private static string GetEmailBody(string type, KeyValuePair<string, object[]> template)
    {
        var body = type switch
        {
            "html" => string.Format(InvariantCulture, template.Key, template.Value),
            "text" => string.Format(InvariantCulture, template.Key, template.Value),
            _ => throw new ArgumentException(
                string.Format(
                    InvariantCulture,
                    CompositeFormat.Parse(Exceptions.UnsupportedEmailBodyType).Format,
                    type),
                nameof(type)),
        };

        return body;
    }

    private static Mock<IOptions<EmailerOptions>> GetMockEmailerOptions(bool hasCredentials = true)
    {
        var mockOptions = new Mock<IOptions<EmailerOptions>>();
        var options = new EmailerOptions()
        {
            FeedbackEmailAddress = EmailAddress,
            QueryStringParameterName = QueryStringParameterName,
            RejectRegistrationUrl = RejectRegistrationUrl,
            ResetPasswordUrl = ResetPasswordUrl,
            Sender = Sender,
            SmtpPassword = hasCredentials ? SmtpPassword : string.Empty,
            SmtpPort = SmtpPort,
            SmtpServer = SmtpServer,
            SmtpUserName = hasCredentials ? SmtpUserName : string.Empty,
            UseTls = UseTls,
            VerifyEmailUrl = VerifyEmailUrl,
        };
        _ = mockOptions.Setup(m => m.Value)
            .Returns(options);

        return mockOptions;
    }

    private static Mock<IEmailTokenSerializer> GetMockSerializer()
    {
        var mockSerializer = new Mock<IEmailTokenSerializer>();
        _ = mockSerializer.Setup(m => m.Deserialize(SerializedToken))
            .Returns(new Tuple<string, string, string>(Pepper, Token, SmtpUserName));
        _ = mockSerializer.Setup(m => m.Serialize(Pepper, Token, SmtpUserName))
            .Returns(SerializedToken);

        return mockSerializer;
    }

    private static Mock<ISmtpClient> GetMockSmtpClient(
        KeyValuePair<string, object[]>? htmlTemplate = default,
        KeyValuePair<string, object[]>? textTemplate = default,
        string? subject = default,
        CancellationToken cancellationToken = default)
    {
        var mockClient = new Mock<ISmtpClient>();
        _ = mockClient.Setup(m => m.AuthenticateAsync(SmtpUserName, SmtpPassword, cancellationToken))
            .Returns(Task.CompletedTask);
        _ = mockClient.Setup(m => m.ConnectAsync(SmtpServer, SmtpPort, UseTls, cancellationToken))
            .Returns(Task.CompletedTask);
        _ = mockClient.Setup(m => m.DisconnectAsync(true, cancellationToken))
            .Returns(Task.CompletedTask);
        _ = mockClient.Setup(m => m.SendAsync(It.IsAny<MimeMessage>(), cancellationToken, null))
            .Callback<MimeMessage, CancellationToken, ITransferProgress>(
                (m, ct, _) =>
                {
                    Assert.IsNotNull(htmlTemplate);
                    Assert.IsNotNull(textTemplate);
                    Assert.AreEqual(GetEmailBody("html", htmlTemplate!.Value), m.HtmlBody);
                    Assert.IsTrue(m.From.Mailboxes.Any(ma => ma.Address == Sender));
                    Assert.IsTrue(m.To.Mailboxes.Any(ma => ma.Address == EmailAddress));
                    Assert.AreEqual(subject, m.Subject);
                    Assert.AreEqual(GetEmailBody("text", textTemplate!.Value), m.TextBody);
                })
            .ReturnsAsync(string.Empty);

        return mockClient;
    }

    private static Emailer GetTarget(
        Mock<IOptions<EmailerOptions>>? mockOptions = default,
        Mock<IEmailTokenSerializer>? mockSerializer = default,
        Mock<ISmtpClient>? mockSmtpClient = default)
    {
        return new Emailer(
            (mockOptions ?? GetMockEmailerOptions()).Object,
            (mockSerializer ?? GetMockSerializer()).Object,
            (mockSmtpClient ?? GetMockSmtpClient()).Object,
            new EmailTokenDataValidator());
    }

    private static Task RunAndAssertSendFeedbackEmailTest(bool useCredentials = true)
    {
        var subject = Resources.SendFeedbackSubject;
        var htmlParameters = new KeyValuePair<string, object[]>(
            Resources.SendFeedbackHtml.ReplaceLineEndings(),
            new object[] { subject, EmailAddress, Name, Feedback });
        var textParameters = new KeyValuePair<string, object[]>(
            Resources.SendFeedbackText.ReplaceLineEndings(),
            new object[] { Name, EmailAddress, Feedback });

        return RunAndAssertTest(
            (e, ct) => e.SendFeedbackEmail(Name, Feedback, EmailAddress, ct),
            htmlParameters,
            textParameters,
            subject,
            useCredentials);
    }

    private static Task RunAndAssertSendForgotPasswordEmailTest(
        bool doesLinkExpire = true,
        bool useCredentials = true)
    {
        var subject = Resources.ForgotPasswordSubject;
        var validityText = doesLinkExpire
            ? string.Format(InvariantCulture, CompositeFormat.Parse(Resources.ValidityPeriodText).Format, "1 day")
            : string.Empty;
        var htmlParameters = new KeyValuePair<string, object[]>(
            Resources.ForgotPasswordHtml.ReplaceLineEndings(),
            new object[] { subject, ResetPasswordUrl, validityText });
        var textParameters = new KeyValuePair<string, object[]>(
            Resources.ForgotPasswordText.ReplaceLineEndings(),
            new object[] { ResetPasswordUrl, validityText });
        var tokenData = new EmailTokenData(Pepper, Token, SmtpUserName, doesLinkExpire ? TimeSpan.FromDays(1) : null);

        return RunAndAssertTest(
            (e, ct) => e.SendForgotPasswordEmail(EmailAddress, Name, tokenData, ct),
            htmlParameters,
            textParameters,
            subject,
            useCredentials);
    }

    private static Task RunAndAssertSendVerificationEmailTest(
        bool doesLinkExpire = true,
        bool useCredentials = true)
    {
        var subject = Resources.VerifyEmailSubject;
        var validityText = doesLinkExpire
            ? string.Format(InvariantCulture, CompositeFormat.Parse(Resources.ValidityPeriodText).Format, "1 day")
            : string.Empty;
        var htmlParameters = new KeyValuePair<string, object[]>(
            Resources.VerifyEmailHtml.ReplaceLineEndings(),
            new object[] { subject, VerifyEmailUrl, validityText, RejectRegistrationUrl });
        var textParameters = new KeyValuePair<string, object[]>(
            Resources.VerifyEmailText.ReplaceLineEndings(),
            new object[] { VerifyEmailUrl, validityText, RejectRegistrationUrl });
        var tokenData = new EmailTokenData(Pepper, Token, SmtpUserName, doesLinkExpire ? TimeSpan.FromDays(1) : null);

        return RunAndAssertTest(
            (e, ct) => e.SendVerificationEmail(EmailAddress, Name, tokenData, ct),
            htmlParameters,
            textParameters,
            subject,
            useCredentials);
    }

    private static async Task RunAndAssertTest(
        Func<IEmailer, CancellationToken, Task> func,
        KeyValuePair<string, object[]> htmlParameters,
        KeyValuePair<string, object[]> textParameters,
        string subject,
        bool useCredentials = true)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockSerializer = GetMockSerializer();
        var mockSmtpClient = GetMockSmtpClient(htmlParameters, textParameters, subject, cancellationToken);
        var target = GetTarget(GetMockEmailerOptions(useCredentials), mockSerializer, mockSmtpClient);

        await func(target, cancellationToken).ConfigureAwait(false);
    }
}
