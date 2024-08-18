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

using AutoMapper;
using FluentValidation;
using Hirameku.Authentication;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.IdentityService;
using Hirameku.Registration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Threading;
using ExceptionsProfile = Hirameku.Authentication.ExceptionsProfile;
using SignInResult = Hirameku.Authentication.SignInResult;

[TestClass]
public class AuthenticationControllerTests
{
    private const string Accept = nameof(Accept);
    private const string ContentEncoding = nameof(ContentEncoding);
    private const string ContentLanguage = nameof(ContentLanguage);
    private const string RecaptchaResponse = nameof(RecaptchaResponse);
    private const string RemoteIP = "127.0.0.1";
    private const string UserAgent = nameof(UserAgent);
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationController_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task AuthenticationController_RenewToken_BadRequest()
    {
        var mockHandler = new Mock<IRenewTokenHandler>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockHandler.Setup(
            m => m.RenewToken(It.IsAny<AuthenticationData<RenewTokenModel>>(), cancellationToken))
            .ThrowsAsync(new ValidationException("error"));
        var target = GetTarget(GetControllerContext());
        var result = await target.RenewToken(mockHandler.Object, new RenewTokenModel(), cancellationToken)
            .ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task AuthenticationController_RenewToken_Ok()
    {
        var expected = new JwtSecurityToken();

        var actionResult = await RunRenewTokenTest(AuthenticationResult.Authenticated, expected).ConfigureAwait(false);

        var objectResult = actionResult as OkObjectResult;
        var actual = objectResult?.Value as JwtSecurityToken;

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(AuthenticationResult.LockedOut)]
    [DataRow(AuthenticationResult.NotAuthenticated)]
    [DataRow(AuthenticationResult.PasswordExpired)]
    [DataRow(AuthenticationResult.Suspended)]
    public async Task AuthenticationController_RenewToken_Unauthorized(AuthenticationResult authenticationResult)
    {
        var actionResult = await RunRenewTokenTest(authenticationResult).ConfigureAwait(false);

        var unauthorizedResult = actionResult as UnauthorizedObjectResult;

        Assert.AreEqual(authenticationResult, unauthorizedResult?.Value);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task AuthenticationController_ResetPassword_BadRequest()
    {
        var mockHandler = new Mock<IResetPasswordHandler>();
        var model = new ResetPasswordModel();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockHandler.Setup(
            m => m.ResetPassword(model, nameof(AuthenticationController.ResetPassword), RemoteIP, cancellationToken))
            .ThrowsAsync(new ValidationException("error"));
        var target = GetTarget(GetControllerContext());

        var result = await target.ResetPassword(mockHandler.Object, model, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task AuthenticationController_ResetPassword_Ok()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockHandler = new Mock<IResetPasswordHandler>();
        var model = new ResetPasswordModel()
        {
            Password = nameof(ResetPasswordModel.Password),
            RecaptchaResponse = nameof(ResetPasswordModel.RecaptchaResponse),
            SerializedToken = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(nameof(ResetPasswordModel.SerializedToken))),
        };
        const ResetPasswordResult Expected = ResetPasswordResult.PasswordReset;
        _ = mockHandler.Setup(m => m.ResetPassword(
            model,
            nameof(AuthenticationController.ResetPassword),
            RemoteIP,
            cancellationToken))
            .ReturnsAsync(Expected);
        var target = GetTarget(GetControllerContext());

        var actionResult = await target.ResetPassword(mockHandler.Object, model, cancellationToken)
            .ConfigureAwait(false);

        var objectResult = actionResult as OkObjectResult;
        Assert.IsNotNull(objectResult);
        Assert.AreEqual(Expected, objectResult.Value);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task AuthenticationController_SendPasswordReset_Accepted()
    {
        var model = new SendPasswordResetModel()
        {
            RecaptchaResponse = RecaptchaResponse,
            UserName = UserName,
        };
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockHandler = new Mock<ISendPasswordResetHandler>();
        const string Action = nameof(AuthenticationController.SendPasswordReset);
        _ = mockHandler.Setup(
            m => m.SendPasswordReset(model, Action, RemoteIP, cancellationToken))
            .Returns(Task.CompletedTask);
        var target = GetTarget(GetControllerContext());

        var result = await target.SendPasswordReset(mockHandler.Object, model, cancellationToken)
            .ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(AcceptedResult));
        mockHandler.VerifyAll();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task AuthenticationController_SendPasswordReset_BadRequest()
    {
        var mockHandler = new Mock<ISendPasswordResetHandler>();
        var model = new SendPasswordResetModel();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockHandler.Setup(
            m => m.SendPasswordReset(
                model,
                nameof(AuthenticationController.SendPasswordReset),
                RemoteIP,
                cancellationToken))
            .ThrowsAsync(new ValidationException("error"));
        var target = GetTarget(GetControllerContext());

        var result = await target.SendPasswordReset(mockHandler.Object, model, cancellationToken)
            .ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task AuthenticationController_SignIn_BadRequest()
    {
        var mockHandler = new Mock<ISignInHandler>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockHandler.Setup(m => m.SignIn(It.IsAny<AuthenticationData<SignInModel>>(), cancellationToken))
            .ThrowsAsync(new ValidationException("error"));
        var target = GetTarget(GetControllerContext());

        var result = await target.SignIn(mockHandler.Object, new SignInModel(), cancellationToken)
            .ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(AuthenticationResult.Authenticated)]
    [DataRow(AuthenticationResult.PasswordExpired)]
    public async Task AuthenticationController_SignIn_Ok(AuthenticationResult authenticationResult)
    {
        var sessionToken = new JwtSecurityToken();
        var persistentToken = new PersistentTokenModel();

        var actionResult = await RunSignInTest(
            authenticationResult,
            new TokenResponseModel(sessionToken, persistentToken))
            .ConfigureAwait(false);

        var objectResult = actionResult as OkObjectResult;
        var actual = objectResult?.Value as TokenResponseModel;

        Assert.AreEqual(persistentToken, actual?.PersistentToken);
        Assert.AreEqual(sessionToken, actual?.SessionToken);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(AuthenticationResult.NotAuthenticated)]
    [DataRow(AuthenticationResult.Suspended)]
    public async Task AuthenticationController_SignIn_Unauthorized(AuthenticationResult authenticationResult)
    {
        var actionResult = await RunSignInTest(authenticationResult).ConfigureAwait(false);

        var unauthorizedResult = actionResult as UnauthorizedObjectResult;

        Assert.AreEqual(authenticationResult, unauthorizedResult?.Value);
    }

    private static void AssertAuthenticationData<TModel>(AuthenticationData<TModel> data, TModel model)
        where TModel : class
    {
        Assert.AreEqual(Accept, data.Accept);
        Assert.AreEqual(ContentEncoding, data.ContentEncoding);
        Assert.AreEqual(ContentLanguage, data.ContentLanguage);
        Assert.AreEqual(model, data.Model);
        Assert.AreEqual(RemoteIP, data.RemoteIP);
        Assert.AreEqual(UserAgent, data.UserAgent);
    }

    private static ControllerContext GetControllerContext()
    {
        var context = new DefaultHttpContext();
        var headers = context.Request.Headers;
        headers[HeaderNames.Accept] = Accept;
        headers[HeaderNames.ContentEncoding] = ContentEncoding;
        headers[HeaderNames.ContentLanguage] = ContentLanguage;
        headers[HeaderNames.UserAgent] = UserAgent;
        context.Connection.RemoteIpAddress = IPAddress.Parse(RemoteIP);

        return new ControllerContext() { HttpContext = context };
    }

    private static AuthenticationController GetTarget(
        ControllerContext? context = default,
        Mock<IMapper>? mockMapper = default)
    {
        var mapper = mockMapper?.Object;

        if (mapper == null)
        {
            var configuration = new MapperConfiguration(
                c =>
                {
                    c.AddProfile<ExceptionsProfile>();
                    c.AddProfile<RegistrationProfile>();
                });

            mapper = configuration.CreateMapper();
        }

        return new AuthenticationController(mapper)
        {
            ControllerContext = context ?? new ControllerContext(),
        };
    }

    private static Task<IActionResult> RunRenewTokenTest(
        AuthenticationResult authenticationResult,
        JwtSecurityToken? sessionToken = default)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockHandler = new Mock<IRenewTokenHandler>();
        var renewTokenResult = new RenewTokenResult(authenticationResult, sessionToken);
        var model = new RenewTokenModel()
        {
            ClientId = nameof(RenewTokenModel.ClientId),
            ClientToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(nameof(RenewTokenModel.ClientToken))),
            UserId = "1234567890abcdef12345678",
        };
        _ = mockHandler.Setup(m => m.RenewToken(It.IsAny<AuthenticationData<RenewTokenModel>>(), cancellationToken))
            .Callback<AuthenticationData<RenewTokenModel>, CancellationToken>(
                (d, ct) => AssertAuthenticationData(d, model))
            .ReturnsAsync(renewTokenResult);
        var target = GetTarget(GetControllerContext());

        return target.RenewToken(mockHandler.Object, model, cancellationToken);
    }

    private static Task<IActionResult> RunSignInTest(
        AuthenticationResult authenticationResult,
        TokenResponseModel? responseModel = default)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockHandler = new Mock<ISignInHandler>();
        var signInResult = new SignInResult(authenticationResult);
        var model = new SignInModel()
        {
            Password = nameof(SignInModel.Password),
            UserName = nameof(SignInModel.UserName),
        };
        _ = mockHandler.Setup(m => m.SignIn(It.IsAny<AuthenticationData<SignInModel>>(), cancellationToken))
            .Callback<AuthenticationData<SignInModel>, CancellationToken>((d, ct) => AssertAuthenticationData(d, model))
            .ReturnsAsync(signInResult);
        var mockMapper = new Mock<IMapper>();
        _ = mockMapper.Setup(m => m.Map<TokenResponseModel>(signInResult))
            .Returns(responseModel ?? new TokenResponseModel(new JwtSecurityToken()));
        var target = GetTarget(GetControllerContext(), mockMapper);

        return target.SignIn(mockHandler.Object, model, cancellationToken);
    }
}
