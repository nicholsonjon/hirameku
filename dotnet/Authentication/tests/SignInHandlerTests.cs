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
using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.TestTools;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;

[TestClass]
public class SignInHandlerTests
{
    private const string Accept = nameof(Accept);
    private const string ClientId = nameof(ClientId);
    private const string ClientToken = "Q2xpZW50VG9rZW4=";
    private const int ClientTokenLength = 11;
    private const string ContentEncoding = nameof(ContentEncoding);
    private const string ContentLanguage = nameof(ContentLanguage);
    private const string EmailAddress = nameof(EmailAddress);
    private const int MaxPasswordAttempts = 10;
    private const string Name = nameof(Name);
    private const string Password = TestData.Password;
    private const string RemoteIP = "127.0.0.1";
    private const string UserAgent = nameof(UserAgent);
    private const string UserId = "1234567890abcdef12345678";
    private const string UserName = nameof(UserName);
    private static readonly string Hash = TestUtilities.GetMD5HexString(
        Accept,
        ContentEncoding,
        ContentLanguage,
        RemoteIP,
        UserAgent);

    private static readonly DateTime ExpirationDate = DateTime.UtcNow + TimeSpan.FromDays(365);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SignInHandler_Constructor()
    {
        var target = new SignInHandler(
            Mock.Of<IDocumentDao<AuthenticationEvent>>(),
            Mock.Of<IOptions<AuthenticationOptions>>(),
            Mock.Of<ICacheClient>(),
            Mock.Of<ICachedValueDao>(),
            Mock.Of<IMapper>(),
            Mock.Of<IPasswordDao>(),
            Mock.Of<IPersistentTokenIssuer>(),
            Mock.Of<ISecurityTokenIssuer>(),
            Mock.Of<IValidator<SignInModel>>(),
            Mock.Of<IDocumentDao<UserDocument>>());

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(true)]
    [DataRow(false)]
    public async Task SignInHandler_SignIn_Authenticated(bool rememberMe)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var authenticationResult = AuthenticationResult.Authenticated;
        var mockAuthenticationEventDao = GetMockAuthenticationEventDao(authenticationResult, cancellationToken);

        var result = await RunSignInTest(
            rememberMe: rememberMe,
            mockAuthenticationEventDao: mockAuthenticationEventDao,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var persistentToken = result.PersistentToken;

        mockAuthenticationEventDao.Verify();
        Assert.AreEqual(authenticationResult, result.AuthenticationResult);

        if (rememberMe)
        {
            Assert.AreEqual(ClientId, persistentToken?.ClientId);
            Assert.AreEqual(
                ClientTokenLength,
                Convert.FromBase64String(persistentToken?.ClientToken ?? string.Empty).Length);
            Assert.AreEqual(ExpirationDate, persistentToken?.ExpirationDate);
            Assert.AreEqual(UserId, persistentToken?.UserId);
        }
        else
        {
            Assert.IsNull(persistentToken);
        }

        Assert.AreEqual(
            GetJwtSecurityToken().ToString(),
            result.SessionToken?.ToString() ?? string.Empty);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task AuthentcationRepository_SignIn_LockedOut()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockAuthenticationEventDao = GetMockAuthenticationEventDao(AuthenticationResult.LockedOut, cancellationToken);
        var mockCache = GetMockCache(MaxPasswordAttempts + 1, cancellationToken);

        var result = await RunSignInTest(
            mockAuthenticationEventDao: mockAuthenticationEventDao,
            mockCache: mockCache,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        mockAuthenticationEventDao.Verify();
        mockCache.Verify();
        Assert.AreEqual(AuthenticationResult.LockedOut, result.AuthenticationResult);
        Assert.IsNull(result.PersistentToken);
        Assert.IsNull(result.SessionToken);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(UserStatus.OK)]
    [DataRow(UserStatus.EmailNotVerified)]
    [DataRow(UserStatus.EmailNotVerifiedAndPasswordChangeRequired)]
    [DataRow(UserStatus.PasswordChangeRequired)]
    public async Task AuthentcationRepository_SignIn_NotAuthenticated(UserStatus userStatus)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var authenticationResult = AuthenticationResult.NotAuthenticated;
        var mockAuthenticationEventDao = GetMockAuthenticationEventDao(authenticationResult, cancellationToken);

        var result = await RunSignInTest(
            userStatus,
            passwordResult: PasswordVerificationResult.NotVerified,
            mockAuthenticationEventDao: mockAuthenticationEventDao,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        mockAuthenticationEventDao.Verify();
        Assert.AreEqual(authenticationResult, result.AuthenticationResult);
        Assert.IsNull(result.PersistentToken);
        Assert.IsNull(result.SessionToken);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(UserStatus.EmailNotVerifiedAndPasswordChangeRequired)]
    [DataRow(UserStatus.PasswordChangeRequired)]
    public async Task SignInHandler_SignIn_PasswordChangeRequired(UserStatus userStatus)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var authenticationResult = AuthenticationResult.PasswordExpired;
        var mockAuthenticationEventDao = GetMockAuthenticationEventDao(authenticationResult, cancellationToken);

        var result = await RunSignInTest(
            userStatus,
            passwordResult: PasswordVerificationResult.Verified,
            mockAuthenticationEventDao: mockAuthenticationEventDao,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        mockAuthenticationEventDao.Verify();
        Assert.AreEqual(authenticationResult, result.AuthenticationResult);
        Assert.IsNull(result.PersistentToken);
        Assert.AreEqual(
            GetJwtSecurityToken().ToString(),
            result.SessionToken?.ToString() ?? string.Empty);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task SignInHandler_SignIn_PasswordExpired()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var authenticationResult = AuthenticationResult.PasswordExpired;
        var mockAuthenticationEventDao = GetMockAuthenticationEventDao(authenticationResult, cancellationToken);
        var mockCachedValueDao = GetMockCachedValueDao(UserStatus.PasswordChangeRequired);

        var result = await RunSignInTest(
            passwordResult: PasswordVerificationResult.VerifiedAndExpired,
            mockAuthenticationEventDao: mockAuthenticationEventDao,
            mockCachedValueDao: mockCachedValueDao,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        mockAuthenticationEventDao.Verify();
        mockCachedValueDao.Verify();
        Assert.AreEqual(authenticationResult, result.AuthenticationResult);
        Assert.IsNull(result.PersistentToken);
        Assert.AreEqual(
            GetJwtSecurityToken().ToString(),
            result.SessionToken?.ToString() ?? string.Empty);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task AuthentcationRepository_SignIn_Suspended()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockAuthenticationDao = GetMockAuthenticationEventDao(AuthenticationResult.Suspended, cancellationToken);

        var result = await RunSignInTest(
            UserStatus.Suspended,
            mockAuthenticationEventDao: mockAuthenticationDao,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        mockAuthenticationDao.Verify();
        Assert.AreEqual(AuthenticationResult.Suspended, result.AuthenticationResult);
        Assert.IsNull(result.PersistentToken);
        Assert.IsNull(result.SessionToken);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task SignInHandler_SignIn_DataIsNull_Throws()
    {
        var target = GetTarget();

        _ = await target.SignIn(null!, CancellationToken.None).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task SignInHandler_SignIn_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        _ = await target.SignIn(GetAuthenticationData(new SignInModel()), CancellationToken.None).ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task SignInHandler_SignIn_UserDoesNotExist()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = GetMockUserDao(cancellationToken: cancellationToken);
        var target = GetTarget(mockUserDao: mockUserDao);

        var result = await target.SignIn(GetAuthenticationData(GetSignInModel()), cancellationToken)
            .ConfigureAwait(false);

        Assert.AreEqual(AuthenticationResult.NotAuthenticated, result.AuthenticationResult);
        Assert.IsNull(result.PersistentToken);
        Assert.IsNull(result.SessionToken);
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

    private static Mock<IDocumentDao<AuthenticationEvent>> GetMockAuthenticationEventDao(
        AuthenticationResult authenticationResult,
        CancellationToken cancellationToken = default)
    {
        var mockAuthenticationDao = new Mock<IDocumentDao<AuthenticationEvent>>();
        mockAuthenticationDao.Setup(m => m.Save(It.IsAny<AuthenticationEvent>(), cancellationToken))
            .Callback<AuthenticationEvent, CancellationToken>(
                (ae, ct) =>
                {
                    Assert.AreEqual(Accept, ae.Accept);
                    Assert.AreEqual(authenticationResult, ae.AuthenticationResult);
                    Assert.AreEqual(ContentEncoding, ae.ContentEncoding);
                    Assert.AreEqual(ContentLanguage, ae.ContentLanguage);
                    Assert.AreEqual(Hash, ae.Hash);
                    Assert.AreEqual(string.Empty, ae.Id);
                    Assert.AreEqual(RemoteIP, ae.RemoteIP);
                    Assert.AreEqual(UserAgent, ae.UserAgent);
                    Assert.AreEqual(UserId, ae.UserId);
                })
            .ReturnsAsync(new SaveResult())
            .Verifiable();

        return mockAuthenticationDao;
    }

    private static Mock<IOptions<AuthenticationOptions>> GetMockAuthenticationOptions()
    {
        var mockOptions = new Mock<IOptions<AuthenticationOptions>>();
        mockOptions.Setup(m => m.Value)
            .Returns(new AuthenticationOptions() { MaxPasswordAttempts = MaxPasswordAttempts })
            .Verifiable();

        return mockOptions;
    }

    private static Mock<ICacheClient> GetMockCache(
        int counter = MaxPasswordAttempts,
        CancellationToken cancellationToken = default)
    {
        var mockCache = new Mock<ICacheClient>();
        mockCache.Setup(m => m.IncrementCounter(UserId, cancellationToken))
            .ReturnsAsync(counter)
            .Verifiable();

        return mockCache;
    }

    private static Mock<ICachedValueDao> GetMockCachedValueDao(UserStatus userStatus = UserStatus.OK)
    {
        var mockDao = new Mock<ICachedValueDao>();
        mockDao.Setup(m => m.SetUserStatus(UserId, userStatus))
            .Returns(Task.CompletedTask)
            .Verifiable();

        return mockDao;
    }

    private static Mock<IPasswordDao> GetMockPasswordDao(
        PasswordVerificationResult passwordResult = PasswordVerificationResult.Verified,
        CancellationToken cancellationToken = default)
    {
        var mockPasswordDao = new Mock<IPasswordDao>();
        mockPasswordDao.Setup(m => m.VerifyPassword(UserId, Password, cancellationToken))
            .ReturnsAsync(passwordResult)
            .Verifiable();

        return mockPasswordDao;
    }

    private static Mock<IPersistentTokenIssuer> GetMockPersistentTokenIssuer(
        CancellationToken cancellationToken = default)
    {
        var mockPersistentTokenIssuer = new Mock<IPersistentTokenIssuer>();
        mockPersistentTokenIssuer.Setup(m => m.Issue(UserId, cancellationToken))
            .ReturnsAsync(new PersistentTokenModel()
            {
                ClientId = ClientId,
                ClientToken = ClientToken,
                ExpirationDate = ExpirationDate,
                UserId = UserId,
            })
            .Verifiable();

        return mockPersistentTokenIssuer;
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

    private static SignInModel GetSignInModel(bool rememberMe = false)
    {
        return new SignInModel()
        {
            Password = Password,
            RememberMe = rememberMe,
            UserName = UserName,
        };
    }

    private static SignInHandler GetTarget(
        Mock<IDocumentDao<AuthenticationEvent>>? mockAuthenticationEventDao = default,
        Mock<ICacheClient>? mockCache = default,
        Mock<ICachedValueDao>? mockCachedValueDao = default,
        Mock<IMapper>? mockMapper = default,
        Mock<IPasswordDao>? mockPasswordDao = default,
        Mock<IPasswordValidator>? mockPasswordValidator = default,
        Mock<IPersistentTokenIssuer>? mockPersistentTokenIssuer = default,
        Mock<ISecurityTokenIssuer>? mockSecurityTokenIssuer = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default)
    {
        var mapper = mockMapper?.Object
            ?? new MapperConfiguration(c => c.AddProfile<AuthenticationProfile>()).CreateMapper();
        var passwordValidator = mockPasswordValidator?.Object ?? Mock.Of<IPasswordValidator>();

        return new SignInHandler(
            mockAuthenticationEventDao?.Object ?? Mock.Of<IDocumentDao<AuthenticationEvent>>(),
            GetMockAuthenticationOptions().Object,
            mockCache?.Object ?? Mock.Of<ICacheClient>(),
            mockCachedValueDao?.Object ?? Mock.Of<ICachedValueDao>(),
            mapper,
            mockPasswordDao?.Object ?? Mock.Of<IPasswordDao>(),
            mockPersistentTokenIssuer?.Object ?? Mock.Of<IPersistentTokenIssuer>(),
            mockSecurityTokenIssuer?.Object ?? Mock.Of<ISecurityTokenIssuer>(),
            new SignInModelValidator(),
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

    private static async Task<SignInResult> RunSignInTest(
        UserStatus userStatus = UserStatus.OK,
        PasswordVerificationResult passwordResult = PasswordVerificationResult.Verified,
        bool rememberMe = false,
        Mock<IDocumentDao<AuthenticationEvent>>? mockAuthenticationEventDao = default,
        Mock<ICacheClient>? mockCache = default,
        Mock<ICachedValueDao>? mockCachedValueDao = default,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken == CancellationToken.None)
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
        }

        var mockPasswordDao = GetMockPasswordDao(passwordResult, cancellationToken);
        var mockPersistentTokenIssuer = GetMockPersistentTokenIssuer(cancellationToken: cancellationToken);
        var user = GetUser(userStatus);
        var mockUserDao = GetMockUserDao(user, cancellationToken);
        var target = GetTarget(
            mockAuthenticationEventDao: mockAuthenticationEventDao,
            mockCache: mockCache ?? GetMockCache(cancellationToken: cancellationToken),
            mockCachedValueDao ?? GetMockCachedValueDao(),
            mockPasswordDao: mockPasswordDao,
            mockPersistentTokenIssuer: mockPersistentTokenIssuer,
            mockSecurityTokenIssuer: GetMockSecurityTokenIssuer(user),
            mockUserDao: mockUserDao);

        var signInResult = await target.SignIn(GetAuthenticationData(GetSignInModel(rememberMe)), cancellationToken)
            .ConfigureAwait(false);

        mockUserDao.Verify();

        return signInResult;
    }
}
