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
using Hirameku.Email;
using Hirameku.Recaptcha;
using Hirameku.TestTools;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;

[TestClass]
public class AuthenticationProviderTests
{
    private const string Accept = nameof(Accept);
    private const string Action = nameof(Action);
    private const string ClientId = nameof(ClientId);
    private const string ClientToken = "Q2xpZW50VG9rZW4=";
    private const int ClientTokenLength = 11;
    private const string ContentEncoding = nameof(ContentEncoding);
    private const string ContentLanguage = nameof(ContentLanguage);
    private const string EmailAddress = nameof(EmailAddress);
    private const int MaxPasswordAttempts = 10;
    private const string Name = nameof(Name);
    private const string Password = TestData.Password;
    private const string Pepper = TestData.Pepper;
    private const string RecaptchaResponse = nameof(RecaptchaResponse);
    private const string RemoteIP = "127.0.0.1";
    private const string SerializedToken = TestData.SerializedToken;
    private const string Token = TestData.Token;
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
    private static readonly TimeSpan MaxVerificationAge = TimeSpan.FromDays(1);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationProvider_Constructor()
    {
        var target = GetTarget();

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

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(VerificationTokenVerificationResult.NotVerified, ResetPasswordResult.TokenNotVerified)]
    [DataRow(VerificationTokenVerificationResult.TokenExpired, ResetPasswordResult.TokenExpired)]
    [DataRow(VerificationTokenVerificationResult.Verified, ResetPasswordResult.PasswordReset)]
    public async Task ResetPasswordProvider_ResetPassword(
        VerificationTokenVerificationResult verificationResult,
        ResetPasswordResult expectedResult)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockPasswordDao = GetMockPasswordDao(cancellationToken: cancellationToken);
        var target = GetTarget(
            mockEmailTokenSerializer: GetMockEmailTokenSerializer(),
            mockPasswordDao: mockPasswordDao,
            mockPasswordValidator: GetMockPasswordValidator(cancellationToken: cancellationToken),
            mockRecaptchaResponseValidator: GetMockRecaptchaResponseValidator(cancellationToken: cancellationToken),
            mockUserDao: GetMockUserDao(GetUser(), cancellationToken),
            mockVerificationDao: GetMockVerificationDao(verificationResult, cancellationToken));

        var actualResult = await target.ResetPassword(
            GetResetPasswordModel(),
            Action,
            RemoteIP,
            cancellationToken)
            .ConfigureAwait(false);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task ResetPasswordProvider_ResetPassword_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        _ = await target.ResetPassword(new ResetPasswordModel(), Action, RemoteIP).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task ResetPasswordProvider_ResetPassword_ModelIsNull_Throws()
    {
        var target = GetTarget();

        _ = await target.ResetPassword(null!, Action, RemoteIP).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(UserStatus.EmailNotVerifiedAndPasswordChangeRequired, UserStatus.EmailNotVerified)]
    [DataRow(UserStatus.PasswordChangeRequired, UserStatus.OK)]
    public async Task ResetPasswordProvider_ResetPassword_PasswordChangeRequired(
        UserStatus currentStatus,
        UserStatus newStatus)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCachedValueDao = GetMockCachedValueDao(newStatus);
        var target = GetTarget(
            mockCachedValueDao: mockCachedValueDao,
            mockEmailTokenSerializer: GetMockEmailTokenSerializer(),
            mockPasswordValidator: GetMockPasswordValidator(cancellationToken: cancellationToken),
            mockRecaptchaResponseValidator: GetMockRecaptchaResponseValidator(cancellationToken: cancellationToken),
            mockUserDao: GetMockUserDao(GetUser(currentStatus), cancellationToken),
            mockVerificationDao: GetMockVerificationDao(
                VerificationTokenVerificationResult.Verified,
                cancellationToken));

        _ = await target.ResetPassword(GetResetPasswordModel(), Action, RemoteIP, cancellationToken)
            .ConfigureAwait(false);

        mockCachedValueDao.Verify();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(RecaptchaVerificationFailedException))]
    [DataRow(RecaptchaVerificationResult.InsufficientScore)]
    [DataRow(RecaptchaVerificationResult.InvalidAction)]
    [DataRow(RecaptchaVerificationResult.InvalidHost)]
    [DataRow(RecaptchaVerificationResult.NotVerified)]
    public async Task ResetPasswordProvider_ResetPassword_RecaptchaVerificationFailed_Throws(
        RecaptchaVerificationResult result)
    {
        var mockPasswordValidator = GetMockPasswordValidator();
        var mockRecaptchaValidator = GetMockRecaptchaResponseValidator(result);
        var target = GetTarget(
            mockPasswordValidator: mockPasswordValidator,
            mockRecaptchaResponseValidator: mockRecaptchaValidator);

        _ = await target.ResetPassword(GetResetPasswordModel(), Action, RemoteIP).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task ResetPasswordProvider_ResetPassword_UserDoesNotExist_Throws()
    {
        var target = GetTarget(
            mockEmailTokenSerializer: GetMockEmailTokenSerializer(),
            mockPasswordValidator: GetMockPasswordValidator(),
            mockRecaptchaResponseValidator: GetMockRecaptchaResponseValidator(),
            mockUserDao: GetMockUserDao(null!));

        _ = await target.ResetPassword(GetResetPasswordModel(), Action, RemoteIP).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserSuspendedException))]
    public async Task ResetPasswordProvider_ResetPassword_UserSuspended_Throws()
    {
        var mockUserDao = GetMockUserDao(GetUser(UserStatus.Suspended));
        var target = GetTarget(
            mockEmailTokenSerializer: GetMockEmailTokenSerializer(),
            mockPasswordValidator: GetMockPasswordValidator(),
            mockRecaptchaResponseValidator: GetMockRecaptchaResponseValidator(),
            mockUserDao: mockUserDao);

        _ = await target.ResetPassword(GetResetPasswordModel(), Action, RemoteIP).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(EmailAddressNotVerifiedException))]
    [DataRow(UserStatus.EmailNotVerified)]
    [DataRow(UserStatus.EmailNotVerifiedAndPasswordChangeRequired)]
    public async Task SendPasswordResetProvider_SendPasswordReset_EmailAddressNotVerified_Throws(UserStatus userStatus)
    {
        await RunSendPasswordResetTest(GetUser(userStatus)).ConfigureAwait(false);

        Assert.Fail(nameof(EmailAddressNotVerifiedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task SendPasswordResetProvider_SendPasswordReset_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        await target.SendPasswordReset(new SendPasswordResetModel(), Action, RemoteIP).ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task SendPasswordResetProvider_SendPasswordReset_ModelIsNull_Throws()
    {
        var target = GetTarget();

        await target.SendPasswordReset(null!, Action, RemoteIP).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(RecaptchaVerificationFailedException))]
    public async Task SendPasswordResetProvider_SendPasswordReset_RecaptchaVerificationFailed_Throws()
    {
        await RunSendPasswordResetTest(recaptchaResult: RecaptchaVerificationResult.NotVerified).ConfigureAwait(false);

        Assert.Fail(nameof(RecaptchaVerificationFailedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task SendPasswordResetProvider_SendPasswordReset_UserDoesNotExist_Throws()
    {
        await RunSendPasswordResetTest(null!).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserSuspendedException))]
    public async Task SendPasswordResetProvider_SendPasswordReset_UserSuspended_Throws()
    {
        var user = GetUser(UserStatus.Suspended);

        await RunSendPasswordResetTest(user).ConfigureAwait(false);

        Assert.Fail(nameof(UserSuspendedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(true)]
    [DataRow(false)]
    public async Task AuthenticationProvider_SignIn_Authenticated(bool rememberMe)
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
    public async Task AuthenticationProvider_SignIn_PasswordChangeRequired(UserStatus userStatus)
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
    public async Task AuthenticationProvider_SignIn_PasswordExpired()
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
    public async Task AuthenticationProvider_SignIn_DataIsNull_Throws()
    {
        var target = GetTarget();

        _ = await target.SignIn(null!, CancellationToken.None).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task AuthenticationProvider_SignIn_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        _ = await target.SignIn(GetAuthenticationData(new SignInModel()), CancellationToken.None).ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task AuthenticationProvider_SignIn_UserDoesNotExist()
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

    private static Mock<IEmailTokenSerializer> GetMockEmailTokenSerializer()
    {
        var mockSerializer = new Mock<IEmailTokenSerializer>();
        mockSerializer.Setup(m => m.Deserialize(SerializedToken))
            .Returns(new Tuple<string, string, string>(Pepper, Token, UserName))
            .Verifiable();

        return mockSerializer;
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

    private static Mock<IPasswordValidator> GetMockPasswordValidator(
        PasswordValidationResult result = PasswordValidationResult.Valid,
        CancellationToken cancellationToken = default)
    {
        var mockPasswordValidator = new Mock<IPasswordValidator>();
        mockPasswordValidator.Setup(m => m.Validate(Password, cancellationToken))
            .ReturnsAsync(result)
            .Verifiable();

        return mockPasswordValidator;
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

    private static Mock<IRecaptchaResponseValidator> GetMockRecaptchaResponseValidator(
        RecaptchaVerificationResult result = RecaptchaVerificationResult.Verified,
        CancellationToken cancellationToken = default)
    {
        var mockValidator = new Mock<IRecaptchaResponseValidator>();
        mockValidator.Setup(
            m => m.Validate(RecaptchaResponse, Action, RemoteIP, cancellationToken))
            .ReturnsAsync(result)
            .Verifiable();

        return mockValidator;
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

    private static Mock<IVerificationDao> GetMockVerificationDao(
        VerificationTokenVerificationResult verificationResult = VerificationTokenVerificationResult.Verified,
        CancellationToken cancellationToken = default)
    {
        var mockVerificationDao = new Mock<IVerificationDao>();
        mockVerificationDao.Setup(
            m => m.VerifyToken(UserId, EmailAddress, VerificationType.PasswordReset, Token, Pepper, cancellationToken))
            .ReturnsAsync(verificationResult)
            .Verifiable();

        return mockVerificationDao;
    }

    private static Mock<IOptions<VerificationOptions>> GetMockVerificationOptions()
    {
        var mockOptions = new Mock<IOptions<VerificationOptions>>();
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new VerificationOptions() { MaxVerificationAge = MaxVerificationAge });

        return mockOptions;
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

    private static ResetPasswordModel GetResetPasswordModel()
    {
        return new ResetPasswordModel()
        {
            Password = Password,
            RecaptchaResponse = RecaptchaResponse,
            SerializedToken = SerializedToken,
        };
    }

    private static SendPasswordResetModel GetSendPasswordResetModel()
    {
        return new SendPasswordResetModel()
        {
            RecaptchaResponse = RecaptchaResponse,
            UserName = UserName,
        };
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

    private static AuthenticationProvider GetTarget(
        Mock<IDocumentDao<AuthenticationEvent>>? mockAuthenticationEventDao = default,
        Mock<ICacheClient>? mockCache = default,
        Mock<ICachedValueDao>? mockCachedValueDao = default,
        Mock<IEmailer>? mockEmailer = default,
        Mock<IEmailTokenSerializer>? mockEmailTokenSerializer = default,
        Mock<IMapper>? mockMapper = default,
        Mock<IPasswordDao>? mockPasswordDao = default,
        Mock<IPasswordValidator>? mockPasswordValidator = default,
        Mock<IPersistentTokenDao>? mockPersistentTokenDao = default,
        Mock<IPersistentTokenIssuer>? mockPersistentTokenIssuer = default,
        Mock<IRecaptchaResponseValidator>? mockRecaptchaResponseValidator = default,
        Mock<ISecurityTokenIssuer>? mockSecurityTokenIssuer = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default,
        Mock<IVerificationDao>? mockVerificationDao = default)
    {
        var mapper = mockMapper?.Object
            ?? new MapperConfiguration(c => c.AddProfile<AuthenticationProfile>()).CreateMapper();
        var passwordValidator = mockPasswordValidator?.Object ?? Mock.Of<IPasswordValidator>();

        return new AuthenticationProvider(
            mockAuthenticationEventDao?.Object ?? Mock.Of<IDocumentDao<AuthenticationEvent>>(),
            GetMockAuthenticationOptions().Object,
            mockCache?.Object ?? Mock.Of<ICacheClient>(),
            mockCachedValueDao?.Object ?? Mock.Of<ICachedValueDao>(),
            mockEmailer?.Object ?? Mock.Of<IEmailer>(),
            mockEmailTokenSerializer?.Object ?? Mock.Of<IEmailTokenSerializer>(),
            mapper,
            mockPasswordDao?.Object ?? Mock.Of<IPasswordDao>(),
            mockPersistentTokenDao?.Object ?? Mock.Of<IPersistentTokenDao>(),
            mockPersistentTokenIssuer?.Object ?? Mock.Of<IPersistentTokenIssuer>(),
            mockRecaptchaResponseValidator?.Object ?? Mock.Of<IRecaptchaResponseValidator>(),
            new RenewTokenModelValidator(),
            new ResetPasswordModelValidator(passwordValidator),
            mockSecurityTokenIssuer?.Object ?? Mock.Of<ISecurityTokenIssuer>(),
            new SendPasswordResetModelValidator(),
            new SignInModelValidator(),
            mockUserDao?.Object ?? Mock.Of<IDocumentDao<UserDocument>>(),
            mockVerificationDao?.Object ?? Mock.Of<IVerificationDao>(),
            GetMockVerificationOptions().Object);
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

    private static async Task RunSendPasswordResetTest(
        UserDocument? user = default,
        RecaptchaVerificationResult recaptchaResult = RecaptchaVerificationResult.Verified)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockEmailer = new Mock<IEmailer>();
        var verificationToken = await TestUtilities.GenerateToken().ConfigureAwait(false);
        _ = mockEmailer.Setup(
            m => m.SendVerificationEmail(
                verificationToken.EmailAddress,
                Name,
                new EmailTokenData(verificationToken.Pepper, verificationToken.Token, UserName, MaxVerificationAge),
                cancellationToken))
            .Returns(Task.CompletedTask);
        var mockValidator = GetMockRecaptchaResponseValidator(recaptchaResult, cancellationToken);
        var mockUserDao = GetMockUserDao(user, cancellationToken);
        var mockVerificationDao = GetMockVerificationDao(cancellationToken: cancellationToken);
        _ = mockVerificationDao.Setup(
            m => m.GenerateVerificationToken(
                UserId,
                EmailAddress,
                VerificationType.PasswordReset,
                cancellationToken))
            .ReturnsAsync(verificationToken);
        var target = GetTarget(
            mockEmailer: mockEmailer,
            mockRecaptchaResponseValidator: mockValidator,
            mockUserDao: mockUserDao,
            mockVerificationDao: mockVerificationDao);

        await target.SendPasswordReset(GetSendPasswordResetModel(), Action, RemoteIP, cancellationToken)
            .ConfigureAwait(false);
    }
}
