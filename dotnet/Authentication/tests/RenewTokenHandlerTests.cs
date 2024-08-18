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

namespace Hirameku.Authentication.Tests;

using AutoMapper;
using FluentValidation;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.TestTools;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;

[TestClass]
public class RenewTokenHandlerTests
{
    private const string Accept = nameof(Accept);
    private const string ClientId = nameof(ClientId);
    private const string ClientToken = "Q2xpZW50VG9rZW4=";
    private const string ContentEncoding = nameof(ContentEncoding);
    private const string ContentLanguage = nameof(ContentLanguage);
    private const string EmailAddress = nameof(EmailAddress);
    private const string Name = nameof(Name);
    private const string RemoteIP = "127.0.0.1";
    private const string UserAgent = nameof(UserAgent);
    private const string UserId = "1234567890abcdef12345678";
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RenewTokenHandler_Constructor()
    {
        var target = new RenewTokenHandler(
            Mock.Of<IDocumentDao<AuthenticationEvent>>(),
            Mock.Of<IMapper>(),
            Mock.Of<IPersistentTokenDao>(),
            Mock.Of<IValidator<RenewTokenModel>>(),
            Mock.Of<ISecurityTokenIssuer>(),
            Mock.Of<IDocumentDao<UserDocument>>());

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(PersistentTokenVerificationResult.NoTokenAvailable, AuthenticationResult.NotAuthenticated)]
    [DataRow(PersistentTokenVerificationResult.NotVerified, AuthenticationResult.NotAuthenticated)]
    [DataRow(PersistentTokenVerificationResult.Verified, AuthenticationResult.Authenticated)]
    public async Task RenewTokenProvider_RenewToken(
        PersistentTokenVerificationResult verificationResult,
        AuthenticationResult expectedResult)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var user = GetUser();
        var mockUserDao = GetMockUserDao(user, cancellationToken);
        var mockPersistentTokenDao = GetMockPersistentTokenDao(verificationResult, cancellationToken);
        var target = GetTarget(
            mockSecurityTokenIssuer: GetMockSecurityTokenIssuer(user),
            mockUserDao: mockUserDao,
            mockPersistentTokenDao: mockPersistentTokenDao);

        var result = await target.RenewToken(GetAuthenticationData(GetRenewTokenModel()), cancellationToken)
            .ConfigureAwait(false);

        var sessionToken = result.SessionToken;

        Assert.AreEqual(expectedResult, result.AuthenticationResult);
        Assert.IsTrue(expectedResult == AuthenticationResult.Authenticated
            ? sessionToken != null
            : sessionToken == null);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task RenewTokenProvider_RenewToken_DataIsNull_Throws()
    {
        var target = GetTarget();

        _ = await target.RenewToken(null!).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task RenewTokenProvider_RenewToken_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        _ = await target.RenewToken(GetAuthenticationData(new RenewTokenModel())).ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(UserStatus.EmailNotVerifiedAndPasswordChangeRequired)]
    [DataRow(UserStatus.PasswordChangeRequired)]
    public async Task RenewTokenProvider_RenewToken_PasswordChangeRequired(UserStatus userStatus)
    {
        var mockUserDao = GetMockUserDao(GetUser(userStatus));
        var target = GetTarget(mockUserDao: mockUserDao);

        var result = await target.RenewToken(GetAuthenticationData(GetRenewTokenModel())).ConfigureAwait(false);

        Assert.AreEqual(AuthenticationResult.PasswordExpired, result.AuthenticationResult);
        Assert.IsNull(result.SessionToken);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task RenewTokenProvider_RenewToken_UserDoesNotExist_Throws()
    {
        var target = GetTarget();

        _ = await target.RenewToken(GetAuthenticationData(GetRenewTokenModel())).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserSuspendedException))]
    public async Task RenewTokenProvider_RenewToken_UserSuspended_Throws()
    {
        var mockUserDao = GetMockUserDao(GetUser(UserStatus.Suspended));
        var target = GetTarget(mockUserDao: mockUserDao);

        _ = await target.RenewToken(GetAuthenticationData(GetRenewTokenModel())).ConfigureAwait(false);

        Assert.Fail(nameof(UserSuspendedException) + " expected");
    }

    private static AuthenticationData<TModel> GetAuthenticationData<TModel>(TModel model)
        where TModel : class
    {
        return new AuthenticationData<TModel>(
            Accept,
            ContentEncoding,
            ContentLanguage,
            model,
            RemoteIP,
            UserAgent);
    }

    private static JwtSecurityToken GetJwtSecurityToken()
    {
        return TestUtilities.GetJwtSecurityToken(UserName, UserId, Name);
    }

    private static Mock<IPersistentTokenDao> GetMockPersistentTokenDao(
        PersistentTokenVerificationResult verificationResult = PersistentTokenVerificationResult.Verified,
        CancellationToken cancellationToken = default)
    {
        var mockPersistentTokenDao = new Mock<IPersistentTokenDao>();
        _ = mockPersistentTokenDao.Setup(
            m => m.VerifyPersistentToken(UserId, ClientId, ClientToken, cancellationToken))
            .ReturnsAsync(verificationResult);

        return mockPersistentTokenDao;
    }

    private static Mock<ISecurityTokenIssuer> GetMockSecurityTokenIssuer(User user)
    {
        var mockIssuer = new Mock<ISecurityTokenIssuer>();
        mockIssuer.Setup(m => m.Issue(UserId, user))
            .Returns(GetJwtSecurityToken())
            .Verifiable();

        return mockIssuer;
    }

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDao(
        UserDocument? user = default,
        CancellationToken cancellationToken = default)
    {
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        mockUserDao.Setup(m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, ct) => TestUtilities.AssertExpressionFilter(f, GetUser()))
            .ReturnsAsync(user)
            .Verifiable();

        return mockUserDao;
    }

    private static RenewTokenModel GetRenewTokenModel()
    {
        return new RenewTokenModel()
        {
            ClientId = ClientId,
            ClientToken = ClientToken,
            UserId = UserId,
        };
    }

    private static RenewTokenHandler GetTarget(
        Mock<IDocumentDao<AuthenticationEvent>>? mockAuthenticationEventDao = default,
        Mock<IMapper>? mockMapper = default,
        Mock<IPasswordValidator>? mockPasswordValidator = default,
        Mock<IPersistentTokenDao>? mockPersistentTokenDao = default,
        Mock<ISecurityTokenIssuer>? mockSecurityTokenIssuer = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default)
    {
        var mapper = mockMapper?.Object
            ?? new MapperConfiguration(c => c.AddProfile<AuthenticationProfile>()).CreateMapper();
        var passwordValidator = mockPasswordValidator?.Object ?? Mock.Of<IPasswordValidator>();

        return new RenewTokenHandler(
            mockAuthenticationEventDao?.Object ?? Mock.Of<IDocumentDao<AuthenticationEvent>>(),
            mapper,
            mockPersistentTokenDao?.Object ?? Mock.Of<IPersistentTokenDao>(),
            new RenewTokenModelValidator(),
            mockSecurityTokenIssuer?.Object ?? Mock.Of<ISecurityTokenIssuer>(),
            mockUserDao?.Object ?? Mock.Of<IDocumentDao<UserDocument>>());
    }

    private static UserDocument GetUser(UserStatus userStatus = UserStatus.OK)
    {
        return new UserDocument()
        {
            EmailAddress = EmailAddress,
            Id = UserId,
            Name = Name,
            UserName = UserName,
            UserStatus = userStatus,
        };
    }
}
