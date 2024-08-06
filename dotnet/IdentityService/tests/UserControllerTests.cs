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
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.TestTools;
using Hirameku.User;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

[TestClass]
public class UserControllerTests
{
    private const string EmailAddress = nameof(EmailAddress);
    private const string Name = nameof(Name);
    private const string UserId = "1234567890abcdef12345678";
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserController_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_ChangePassword_BadRequest()
    {
        var mockProvider = new Mock<IUserProvider>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockProvider.Setup(m => m.ChangePassword(It.IsAny<Authenticated<ChangePasswordModel>>(), cancellationToken))
            .ThrowsAsync(new ValidationException("error"));
        var target = GetTarget(TestUtilities.GetUser(), mockProvider);

        var result = await target.ChangePassword(new ChangePasswordModel(), cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_ChangePassword_Ok()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserProvider = new Mock<IUserProvider>();
        var model = new ChangePasswordModel()
        {
            CurrentPassword = nameof(ChangePasswordModel.CurrentPassword),
        };
        var user = TestUtilities.GetUser();
        var expectedResult = new TokenResponseModel(new JwtSecurityToken());
        _ = mockUserProvider.Setup(
            m => m.ChangePassword(It.IsAny<Authenticated<ChangePasswordModel>>(), cancellationToken))
            .Callback<Authenticated<ChangePasswordModel>, CancellationToken>(
                (a, ct) =>
                {
                    Assert.AreEqual(model, a.Model);
                    Assert.AreEqual(user, a.User);
                })
            .ReturnsAsync(expectedResult);
        var target = GetTarget(user, mockUserProvider);

        var result = await target.ChangePassword(model, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        Assert.AreEqual(expectedResult, ((OkObjectResult)result).Value);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_ChangePassword_Unauthorized()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserProvider = new Mock<IUserProvider>();
        _ = mockUserProvider.Setup(
            m => m.ChangePassword(It.IsAny<Authenticated<ChangePasswordModel>>(), cancellationToken))
            .ReturnsAsync(new TokenResponseModel(new JwtSecurityToken()));
        var target = GetTarget(mockUserProvider: mockUserProvider);

        var result = await target.ChangePassword(new ChangePasswordModel(), cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_Delete_NoContent()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserProvider = new Mock<IUserProvider>();
        var user = TestUtilities.GetUser();
        _ = mockUserProvider.Setup(m => m.DeleteUser(It.IsAny<Authenticated<Unit>>(), cancellationToken))
            .Callback<Authenticated<Unit>, CancellationToken>((a, ct) => Assert.AreEqual(user, a.User))
            .Returns(Task.CompletedTask);
        var target = GetTarget(user, mockUserProvider: mockUserProvider);

        var result = await target.Delete(cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(NoContentResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_Delete_Unauthorized()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var target = GetTarget();

        var result = await target.Delete(cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_Get_NotFound()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserProvider = new Mock<IUserProvider>();
        var user = TestUtilities.GetUser();
        _ = mockUserProvider.Setup(m => m.GetUser(It.IsAny<Authenticated<Unit>>(), cancellationToken))
            .Callback<Authenticated<Unit>, CancellationToken>((a, ct) => Assert.AreEqual(user, a.User))
            .ReturnsAsync(null as User);
        var target = GetTarget(user, mockUserProvider: mockUserProvider);

        var result = await target.Get(cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_Get_Ok()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserProvider = new Mock<IUserProvider>();
        var user = TestUtilities.GetUser();
        var expectedResult = new User();
        _ = mockUserProvider.Setup(m => m.GetUser(It.IsAny<Authenticated<Unit>>(), cancellationToken))
            .Callback<Authenticated<Unit>, CancellationToken>((a, ct) => Assert.AreEqual(user, a.User))
            .ReturnsAsync(expectedResult);
        var target = GetTarget(user, mockUserProvider: mockUserProvider);

        var result = await target.Get(cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        Assert.AreEqual(expectedResult, ((OkObjectResult)result).Value);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_Get_Unauthorized()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var target = GetTarget();

        var result = await target.Get(cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_UpdateEmailAddress_BadRequest()
    {
        var mockProvider = new Mock<IUserProvider>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockProvider.Setup(
            m => m.UpdateEmailAddress(It.IsAny<Authenticated<UpdateEmailAddressModel>>(), cancellationToken))
            .ThrowsAsync(new ValidationException("error"));
        var target = GetTarget(TestUtilities.GetUser(), mockProvider);

        var result = await target.UpdateEmailAddress(EmailAddress, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_UpdateEmailAddress_NoContent()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserProvider = new Mock<IUserProvider>();
        var user = TestUtilities.GetUser();
        _ = mockUserProvider.Setup(
            m => m.UpdateEmailAddress(It.IsAny<Authenticated<UpdateEmailAddressModel>>(), cancellationToken))
            .Callback<Authenticated<UpdateEmailAddressModel>, CancellationToken>(
                (a, ct) =>
                {
                    Assert.AreEqual(EmailAddress, a.Model.EmailAddress);
                    Assert.AreEqual(user, a.User);
                })
            .Returns(Task.CompletedTask);
        var target = GetTarget(user, mockUserProvider: mockUserProvider);

        var result = await target.UpdateEmailAddress(EmailAddress, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(NoContentResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_UpdateEmailAddress_Unauthorized()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserProvider = new Mock<IUserProvider>();
        _ = mockUserProvider.Setup(
            m => m.UpdateEmailAddress(It.IsAny<Authenticated<UpdateEmailAddressModel>>(), cancellationToken))
            .Returns(Task.CompletedTask);
        var target = GetTarget(mockUserProvider: mockUserProvider);

        var result = await target.UpdateEmailAddress(EmailAddress, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_UpdateName_BadRequest()
    {
        var mockProvider = new Mock<IUserProvider>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockProvider.Setup(m => m.UpdateName(It.IsAny<Authenticated<UpdateNameModel>>(), cancellationToken))
            .ThrowsAsync(new ValidationException("error"));
        var target = GetTarget(TestUtilities.GetUser(), mockProvider);

        var result = await target.UpdateName(Name, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_UpdateName_Ok()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserProvider = new Mock<IUserProvider>();
        var user = TestUtilities.GetUser();
        var expectedResult = new JwtSecurityToken();
        _ = mockUserProvider.Setup(
            m => m.UpdateName(It.IsAny<Authenticated<UpdateNameModel>>(), cancellationToken))
            .Callback<Authenticated<UpdateNameModel>, CancellationToken>(
                (a, ct) =>
                {
                    Assert.AreEqual(Name, a.Model.Name);
                    Assert.AreEqual(user, a.User);
                })
            .ReturnsAsync(expectedResult);
        var target = GetTarget(user, mockUserProvider: mockUserProvider);

        var result = await target.UpdateName(Name, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        Assert.AreEqual(expectedResult, ((OkObjectResult)result).Value);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_UpdateName_Unauthorized()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserProvider = new Mock<IUserProvider>();
        _ = mockUserProvider.Setup(
            m => m.UpdateName(It.IsAny<Authenticated<UpdateNameModel>>(), cancellationToken))
            .ReturnsAsync(new JwtSecurityToken());
        var target = GetTarget(mockUserProvider: mockUserProvider);

        var result = await target.UpdateName(Name, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_UpdateUserName_BadRequest()
    {
        var mockProvider = new Mock<IUserProvider>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockProvider.Setup(m => m.UpdateUserName(It.IsAny<Authenticated<UpdateUserNameModel>>(), cancellationToken))
            .ThrowsAsync(new ValidationException("error"));
        var target = GetTarget(TestUtilities.GetUser(), mockProvider);

        var result = await target.UpdateUserName(UserName, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_UpdateUserName_Ok()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserProvider = new Mock<IUserProvider>();
        var user = TestUtilities.GetUser();
        var expectedResult = new JwtSecurityToken();
        _ = mockUserProvider.Setup(
            m => m.UpdateUserName(It.IsAny<Authenticated<UpdateUserNameModel>>(), cancellationToken))
            .Callback<Authenticated<UpdateUserNameModel>, CancellationToken>(
                (a, ct) =>
                {
                    Assert.AreEqual(UserName, a.Model.UserName);
                    Assert.AreEqual(user, a.User);
                })
            .ReturnsAsync(expectedResult);
        var target = GetTarget(user, mockUserProvider: mockUserProvider);

        var result = await target.UpdateUserName(UserName, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        Assert.AreEqual(expectedResult, ((OkObjectResult)result).Value);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserController_UpdateUserName_Unauthorized()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserProvider = new Mock<IUserProvider>();
        _ = mockUserProvider.Setup(
            m => m.UpdateUserName(It.IsAny<Authenticated<UpdateUserNameModel>>(), cancellationToken))
            .ReturnsAsync(new JwtSecurityToken());
        var target = GetTarget(mockUserProvider: mockUserProvider);

        var result = await target.UpdateUserName(UserName, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    private static JwtSecurityToken GetJwtSecurityToken()
    {
        return TestUtilities.GetJwtSecurityToken(UserName, UserId, Name);
    }

    private static UserController GetTarget(
        ClaimsPrincipal? user = default,
        Mock<IUserProvider>? mockUserProvider = default)
    {
        var mockPasswordValidator = new Mock<IPasswordValidator>();
        _ = mockPasswordValidator.Setup(m => m.Validate(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordValidationResult.Valid);

        return new UserController(Mock.Of<IMapper>(), mockUserProvider?.Object ?? Mock.Of<IUserProvider>())
        {
            ControllerContext = TestUtilities.GetControllerContext(user),
        };
    }
}
