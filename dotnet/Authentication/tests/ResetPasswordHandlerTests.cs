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
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;

[TestClass]
public class ResetPasswordHandlerTests
{
    private const string Accept = nameof(Accept);
    private const string Action = nameof(Action);
    private const string ContentEncoding = nameof(ContentEncoding);
    private const string ContentLanguage = nameof(ContentLanguage);
    private const string EmailAddress = nameof(EmailAddress);
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

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResetPasswordHandler_Constructor()
    {
        var target = new ResetPasswordHandler(
            Mock.Of<ICachedValueDao>(),
            Mock.Of<IEmailTokenSerializer>(),
            Mock.Of<IMapper>(),
            Mock.Of<IPasswordDao>(),
            Mock.Of<IRecaptchaResponseValidator>(),
            Mock.Of<IValidator<ResetPasswordModel>>(),
            Mock.Of<IDocumentDao<UserDocument>>(),
            Mock.Of<IVerificationDao>());

        Assert.IsNotNull(target);
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

    private static ResetPasswordModel GetResetPasswordModel()
    {
        return new ResetPasswordModel()
        {
            Password = Password,
            RecaptchaResponse = RecaptchaResponse,
            SerializedToken = SerializedToken,
        };
    }

    private static ResetPasswordHandler GetTarget(
        Mock<ICachedValueDao>? mockCachedValueDao = default,
        Mock<IEmailTokenSerializer>? mockEmailTokenSerializer = default,
        Mock<IMapper>? mockMapper = default,
        Mock<IPasswordDao>? mockPasswordDao = default,
        Mock<IPasswordValidator>? mockPasswordValidator = default,
        Mock<IRecaptchaResponseValidator>? mockRecaptchaResponseValidator = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default,
        Mock<IVerificationDao>? mockVerificationDao = default)
    {
        var mapper = mockMapper?.Object
            ?? new MapperConfiguration(c => c.AddProfile<AuthenticationProfile>()).CreateMapper();
        var passwordValidator = mockPasswordValidator?.Object ?? Mock.Of<IPasswordValidator>();

        return new ResetPasswordHandler(
            mockCachedValueDao?.Object ?? Mock.Of<ICachedValueDao>(),
            mockEmailTokenSerializer?.Object ?? Mock.Of<IEmailTokenSerializer>(),
            mapper,
            mockPasswordDao?.Object ?? Mock.Of<IPasswordDao>(),
            mockRecaptchaResponseValidator?.Object ?? Mock.Of<IRecaptchaResponseValidator>(),
            new ResetPasswordModelValidator(passwordValidator),
            mockUserDao?.Object ?? Mock.Of<IDocumentDao<UserDocument>>(),
            mockVerificationDao?.Object ?? Mock.Of<IVerificationDao>());
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
