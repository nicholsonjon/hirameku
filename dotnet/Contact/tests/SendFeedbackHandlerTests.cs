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

namespace Hirameku.Contact.Tests;

using FluentValidation;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Moq;

[TestClass]
public class SendFeedbackHandlerTests
{
    private const string Action = nameof(Action);
    private const string EmailAddress = "test@test.local";
    private const string Feedback = nameof(Feedback);
    private const string Name = nameof(Name);
    private const string RecaptchaResponse = nameof(RecaptchaResponse);
    private const string RemoteIP = nameof(RemoteIP);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SendFeedbackHandler_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task SendFeedbackHandler_SendFeedback()
    {
        var mockRecaptchaResponseValidator = new Mock<IRecaptchaResponseValidator>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockRecaptchaResponseValidator.Setup(
            m => m.Validate(RecaptchaResponse, Action, RemoteIP, cancellationToken))
            .ReturnsAsync(RecaptchaVerificationResult.Verified);
        var target = GetTarget(mockRecaptchaResponseValidator: mockRecaptchaResponseValidator);
        var model = GetModel();

        await target.SendFeedback(model, Action, RemoteIP, cancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task SendFeedbackHandler_SendFeedback_ModelIsInvalid()
    {
        var target = GetTarget();

        await target.SendFeedback(new SendFeedbackModel(), Action, RemoteIP).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task SendFeedbackHandler_SendFeedback_ModelIsNull()
    {
        var target = GetTarget();

        await target.SendFeedback(null!, Action, RemoteIP).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(RecaptchaVerificationFailedException))]
    public async Task SendFeedbackHandler_SendFeedback_RecaptchaVerificationFailed_Throws()
    {
        var mockRecaptchaResponseValidator = new Mock<IRecaptchaResponseValidator>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockRecaptchaResponseValidator.Setup(
            m => m.Validate(RecaptchaResponse, Action, RemoteIP, cancellationToken))
            .ReturnsAsync(RecaptchaVerificationResult.NotVerified);
        var target = GetTarget(mockRecaptchaResponseValidator: mockRecaptchaResponseValidator);
        var model = GetModel();

        await target.SendFeedback(model, Action, RemoteIP, cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(RecaptchaVerificationFailedException) + " expected");
    }

    private static SendFeedbackModel GetModel()
    {
        return new SendFeedbackModel()
        {
            EmailAddress = EmailAddress,
            Feedback = Feedback,
            Name = Name,
            RecaptchaResponse = RecaptchaResponse,
        };
    }

    private static SendFeedbackHandler GetTarget(
        Mock<IEmailer>? mockEmailer = default,
        Mock<IRecaptchaResponseValidator>? mockRecaptchaResponseValidator = default,
        Mock<IValidator<SendFeedbackModel>>? mockSendFeedbackModelValidator = default)
    {
        return new SendFeedbackHandler(
            mockEmailer?.Object ?? Mock.Of<IEmailer>(),
            mockRecaptchaResponseValidator?.Object ?? Mock.Of<IRecaptchaResponseValidator>(),
            mockSendFeedbackModelValidator?.Object ?? new SendFeedbackModelValidator());
    }
}
