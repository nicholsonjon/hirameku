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

namespace Hirameku.User.Tests;

using FluentValidation;
using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.TestTools;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;

[TestClass]
public class ChangePasswordHandlerTests
{
    private const string CurrentPassword = nameof(CurrentPassword);
    private const string NewPassword = nameof(NewPassword);
    private const string UserId = "1234567890abcdef12345678";

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ChangePasswordHandler_Constructor()
    {
        var target = new ChangePasswordHandler(
            Mock.Of<ICachedValueDao>(),
            Mock.Of<IValidator<Authenticated<ChangePasswordModel>>>(),
            Mock.Of<IPasswordDao>(),
            Mock.Of<IPersistentTokenIssuer>(),
            Mock.Of<ISecurityTokenIssuer>(),
            Mock.Of<IDocumentDao<UserDocument>>());

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ChangePasswordHandler_ChangePassword(bool rememberMe)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var user = GetUser();
        var mockUserDao = GetMockUserDao(user, cancellationToken);
        var mockPasswordDao = GetMockPasswordDao(cancellationToken: cancellationToken);
        var mockPersistentTokenIssuer = GetMockPersistentTokenIssuer(cancellationToken);
        var mockSecurityTokenIssuer = GetMockSecurityTokenIssuer(user);
        var target = GetTarget(
            mockPasswordDao: mockPasswordDao,
            mockPersistentTokenIssuer: mockPersistentTokenIssuer,
            mockSecurityTokenIssuer: mockSecurityTokenIssuer,
            mockUserDao: mockUserDao,
            cancellationToken: cancellationToken);

        var responseModel = await target.ChangePassword(GetChangePasswordModel(rememberMe), cancellationToken)
            .ConfigureAwait(false);

        Assert.IsNotNull(responseModel?.SessionToken);
        mockUserDao.Verify();
        mockPasswordDao.Verify();
        mockSecurityTokenIssuer.Verify();

        if (rememberMe)
        {
            var persistentToken = responseModel?.PersistentToken;
            Assert.IsTrue(rememberMe ? persistentToken != null : persistentToken == null);
            mockPersistentTokenIssuer.Verify();
        }
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task ChangePasswordHandler_ChangePassword_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        _ = await target.ChangePassword(
            new Authenticated<ChangePasswordModel>(
                new ChangePasswordModel(),
                new ClaimsPrincipal()))
            .ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(UserStatus.EmailNotVerifiedAndPasswordChangeRequired, UserStatus.EmailNotVerified)]
    [DataRow(UserStatus.PasswordChangeRequired, UserStatus.OK)]
    public async Task ChangePasswordHandler_ChangePassword_PasswordChangeRequired(
        UserStatus currentStatus,
        UserStatus updatedStatus)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var user = GetUser(currentStatus);
        var mockUserDao = GetMockUserDao(user, cancellationToken);
        var mockPasswordDao = GetMockPasswordDao(cancellationToken: cancellationToken);
        var mockPersistentTokenIssuer = GetMockPersistentTokenIssuer(cancellationToken);
        var mockSecurityTokenIssuer = GetMockSecurityTokenIssuer(user);
        var mockCachedValueDao = new Mock<ICachedValueDao>();
        mockCachedValueDao.Setup(m => m.SetUserStatus(UserId, updatedStatus))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var target = GetTarget(
            mockCachedValueDao,
            mockPasswordDao: mockPasswordDao,
            mockPersistentTokenIssuer: mockPersistentTokenIssuer,
            mockSecurityTokenIssuer: mockSecurityTokenIssuer,
            mockUserDao: mockUserDao,
            cancellationToken: cancellationToken);

        var responseModel = await target.ChangePassword(GetChangePasswordModel(), cancellationToken)
            .ConfigureAwait(false);

        Assert.IsNotNull(responseModel?.SessionToken);
        mockUserDao.Verify();
        mockPasswordDao.Verify();
        mockSecurityTokenIssuer.Verify();
        mockCachedValueDao.Verify();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidPasswordException))]
    public async Task ChangePasswordHandler_ChangePassword_PasswordNotVerified_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = GetMockUserDao(GetUser(), cancellationToken);
        var mockPasswordDao = GetMockPasswordDao(PasswordVerificationResult.NotVerified, cancellationToken);
        var target = GetTarget(
            mockPasswordDao: mockPasswordDao,
            mockUserDao: mockUserDao,
            cancellationToken: cancellationToken);

        _ = await target.ChangePassword(GetChangePasswordModel(), cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(InvalidPasswordException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task ChangePasswordHandler_ChangePassword_UserDoesNotExist_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = GetMockUserDao(cancellationToken: cancellationToken);
        var target = GetTarget(cancellationToken: cancellationToken);

        _ = await target.ChangePassword(GetChangePasswordModel(), cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    private static Authenticated<ChangePasswordModel> GetChangePasswordModel(bool rememberMe = false)
    {
        return new Authenticated<ChangePasswordModel>(
            new ChangePasswordModel()
            {
                CurrentPassword = CurrentPassword,
                NewPassword = NewPassword,
                RememberMe = rememberMe,
            },
            new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, UserId) })));
    }

    private static Mock<IPasswordDao> GetMockPasswordDao(
        PasswordVerificationResult result = PasswordVerificationResult.Verified,
        CancellationToken cancellationToken = default)
    {
        var mockDao = new Mock<IPasswordDao>();
        mockDao.Setup(m => m.VerifyPassword(UserId, CurrentPassword, cancellationToken))
            .ReturnsAsync(result)
            .Verifiable();

        return mockDao;
    }

    private static Mock<IPersistentTokenIssuer> GetMockPersistentTokenIssuer(
        CancellationToken cancellationToken = default)
    {
        var mockIssuer = new Mock<IPersistentTokenIssuer>();
        mockIssuer.Setup(m => m.Issue(UserId, cancellationToken))
            .ReturnsAsync(new PersistentTokenModel())
            .Verifiable();

        return mockIssuer;
    }

    private static Mock<ISecurityTokenIssuer> GetMockSecurityTokenIssuer(User user)
    {
        var mockIssuer = new Mock<ISecurityTokenIssuer>();
        mockIssuer.Setup(m => m.Issue(UserId, user))
            .Returns(new JwtSecurityToken())
            .Verifiable();

        return mockIssuer;
    }

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDao(
        UserDocument? user = default,
        CancellationToken cancellationToken = default)
    {
        var mockDao = new Mock<IDocumentDao<UserDocument>>();
        mockDao.Setup(m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, _) => TestUtilities.AssertExpressionFilter(f, user ?? GetUser()))
            .ReturnsAsync(user)
            .Verifiable();

        return mockDao;
    }

    private static ChangePasswordHandler GetTarget(
        Mock<ICachedValueDao>? mockCachedValueDao = default,
        Mock<IPasswordDao>? mockPasswordDao = default,
        Mock<IPasswordValidator>? mockPasswordValidator = default,
        Mock<IPersistentTokenIssuer>? mockPersistentTokenIssuer = default,
        Mock<ISecurityTokenIssuer>? mockSecurityTokenIssuer = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default,
        CancellationToken cancellationToken = default)
    {
        mockPasswordValidator = new Mock<IPasswordValidator>();
        _ = mockPasswordValidator.Setup(m => m.Validate(NewPassword, cancellationToken))
            .ReturnsAsync(PasswordValidationResult.Valid);

        return new ChangePasswordHandler(
            mockCachedValueDao?.Object ?? Mock.Of<ICachedValueDao>(),
            new AuthenticatedValidator<ChangePasswordModel>(
                new ChangePasswordModelValidator(mockPasswordValidator.Object)),
            mockPasswordDao?.Object ?? Mock.Of<IPasswordDao>(),
            mockPersistentTokenIssuer?.Object ?? Mock.Of<IPersistentTokenIssuer>(),
            mockSecurityTokenIssuer?.Object ?? Mock.Of<ISecurityTokenIssuer>(),
            mockUserDao?.Object ?? Mock.Of<IDocumentDao<UserDocument>>());
    }

    private static UserDocument GetUser(UserStatus userStatus = UserStatus.OK)
    {
        return new UserDocument()
        {
            Id = UserId,
            UserStatus = userStatus,
        };
    }
}
