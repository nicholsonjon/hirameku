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

namespace Hirameku.IdentityService.Tests;

using FluentValidation;
using Hirameku.Common.Service;
using Hirameku.Recaptcha;
using Hirameku.Registration;
using Hirameku.TestTools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using CommonPasswordValidationResult = Hirameku.Common.Service.PasswordValidationResult;
using RegistrationPasswordValidationResult = Hirameku.Registration.PasswordValidationResult;

[TestClass]
public class RegistrationControllerTests
{
    private const string EmailAddress = nameof(EmailAddress);
    private const string Password = TestData.Password;
    private const string RecaptchaResponse = nameof(RecaptchaResponse);
    private const string RemoteIP = "127.0.0.1";
    private const string SerializedToken = "U2VyaWFsaXplZFRva2Vu";
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegistrationController_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationController_IsUserNameAvailable_BadRequest()
    {
        var mockProvider = new Mock<IRegistrationProvider>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockProvider.Setup(m => m.IsUserNameAvailable(UserName, cancellationToken))
            .ThrowsAsync(new ValidationException("error"));
        var target = GetTarget(mockProvider);

        var result = await target.IsUserNameAvailable(UserName, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationController_IsUserNameAvailable_Ok()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockProvider = new Mock<IRegistrationProvider>();
        const bool Expected = true;
        _ = mockProvider.Setup(m => m.IsUserNameAvailable(UserName, cancellationToken))
            .ReturnsAsync(Expected);
        var target = GetTarget(mockProvider);

        var actual = await target.IsUserNameAvailable(UserName, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(Expected, (actual as OkObjectResult)?.Value);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationController_Register_Accepted()
    {
        var model = new RegisterModel()
        {
            EmailAddress = EmailAddress,
            Name = nameof(RegisterModel.Name),
            Password = Password,
            RecaptchaResponse = RecaptchaResponse,
            UserName = nameof(RegisterModel.UserName),
        };
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockProvider = new Mock<IRegistrationProvider>();
        _ = mockProvider.Setup(
            m => m.Register(model, nameof(RegistrationController.Register), RemoteIP, cancellationToken))
            .Returns(Task.CompletedTask);
        var target = GetTarget(mockProvider);

        var result = await target.Register(model, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(AcceptedResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationController_Register_BadRequest_RecaptchaVerificationFailedException()
    {
        var result = await RunRegisterBadRequestTest(new RecaptchaVerificationFailedException()).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationController_Register_BadRequest_ValidationException()
    {
        var result = await RunRegisterBadRequestTest(new ValidationException("error")).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationController_RejectRegistration_BadRequest()
    {
        var mockProvider = new Mock<IRegistrationProvider>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockProvider.Setup(m => m.RejectRegistration(SerializedToken, cancellationToken))
            .ThrowsAsync(new ValidationException("error"));
        var target = GetTarget(mockProvider);

        var result = await target.RejectRegistration(SerializedToken, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationController_RejectRegistration_NoContent()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockProvider = new Mock<IRegistrationProvider>();
        _ = mockProvider.Setup(m => m.RejectRegistration(SerializedToken, cancellationToken))
            .Returns(Task.CompletedTask);
        var target = GetTarget(mockProvider);

        var result = await target.RejectRegistration(SerializedToken, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(NoContentResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationController_ResendVerificationEmail_BadRequest()
    {
        var mockProvider = new Mock<IRegistrationProvider>();
        var model = new ResendVerificationEmailModel();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockProvider.Setup(
            m => m.ResendVerificationEmail(
                model,
                nameof(RegistrationController.ResendVerificationEmail),
                RemoteIP,
                cancellationToken))
            .ThrowsAsync(new ValidationException("error"));
        var target = GetTarget(mockProvider);

        var result = await target.ResendVerificationEmail(model, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationController_ResendVerificationEmail_Ok()
    {
        var model = new ResendVerificationEmailModel()
        {
            EmailAddress = EmailAddress,
            RecaptchaResponse = RecaptchaResponse,
        };
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockProvider = new Mock<IRegistrationProvider>();
        var expected = new ResendVerificationEmailResult(default, default, default);
        _ = mockProvider.Setup(
            m => m.ResendVerificationEmail(
                model,
                nameof(RegistrationController.ResendVerificationEmail),
                RemoteIP,
                cancellationToken))
            .ReturnsAsync(expected);
        var target = GetTarget(mockProvider);

        var actual = await target.ResendVerificationEmail(model, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expected, (actual as OkObjectResult)?.Value);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationController_ValidatePassword()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockProvider = new Mock<IRegistrationProvider>();
        var expected = RegistrationPasswordValidationResult.Valid;
        _ = mockProvider.Setup(m => m.ValidatePassword(Password, cancellationToken))
            .ReturnsAsync(expected);
        var target = GetTarget(mockProvider);

        var actual = await target.ValidatePassword(Password, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expected, (actual as OkObjectResult)?.Value);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationController_VerifyEmailAddress_BadRequest()
    {
        var mockProvider = new Mock<IRegistrationProvider>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockProvider.Setup(m => m.VerifyEmaiAddress(SerializedToken, cancellationToken))
            .ThrowsAsync(new ValidationException("error"));
        var target = GetTarget(mockProvider);

        var result = await target.VerifyEmailAddress(SerializedToken, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationController_VerifyEmailAddress_Ok()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockProvider = new Mock<IRegistrationProvider>();
        var expected = EmailVerificationResult.Verified;
        _ = mockProvider.Setup(m => m.VerifyEmaiAddress(SerializedToken, cancellationToken))
            .ReturnsAsync(expected);
        var target = GetTarget(mockProvider);

        var actual = await target.VerifyEmailAddress(SerializedToken, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expected, (actual as OkObjectResult)?.Value);
    }

    private static RegistrationController GetTarget(Mock<IRegistrationProvider>? mockRegistrationProvider = default)
    {
        var mockContextAccessor = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(RemoteIP);
        _ = mockContextAccessor.Setup(m => m.HttpContext)
            .Returns(context);
        var mockPasswordValidator = new Mock<IPasswordValidator>();
        _ = mockPasswordValidator.Setup(m => m.Validate(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommonPasswordValidationResult.Valid);

        return new RegistrationController(
            mockContextAccessor.Object,
            mockRegistrationProvider?.Object ?? Mock.Of<IRegistrationProvider>());
    }

    private static Task<IActionResult> RunRegisterBadRequestTest(Exception exception)
    {
        var mockProvider = new Mock<IRegistrationProvider>();
        var model = new RegisterModel();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockProvider.Setup(
            m => m.Register(model, nameof(RegistrationController.Register), RemoteIP, cancellationToken))
            .ThrowsAsync(exception);
        var target = GetTarget(mockProvider);

        return target.Register(model, cancellationToken);
    }
}
