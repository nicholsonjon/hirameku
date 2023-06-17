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

namespace Hirameku.Registration.Tests;

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
using System.Linq.Expressions;
using System.Security.Cryptography;
using CommonPasswordValidationResult = Hirameku.Common.Service.PasswordValidationResult;
using ServicePasswordValidationResult = Hirameku.Registration.PasswordValidationResult;

[TestClass]
public class RegistrationProviderTests
{
    private const string Action = nameof(Action);
    private const string EmailAddress = "test@test.local";
    private const string Name = nameof(Name);
    private const string Password = TestData.Password;
    private const string Pepper = TestData.Pepper;
    private const int PepperLength = 32;
    private const string RecaptchaResponse = nameof(RecaptchaResponse);
    private const string SerializedToken = TestData.SerializedToken;
    private const string Token = TestData.Token;
    private const string RemoteIP = "127.0.0.1";
    private const string UserId = nameof(UserId);
    private const string UserName = nameof(UserName);
    private static readonly DateTime? ExpireTime = DateTime.UtcNow + TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxVerificationAge = TimeSpan.FromDays(1);
    private static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(5);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegistrationProvider_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationProvider_IsUserNameAvailable()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = GetMockUserDao(new UserDocument() { UserName = UserName }, cancellationToken);
        var target = GetTarget(mockUserDao: mockUserDao);

        var isUserNameAvailable = await target.IsUserNameAvailable(UserName, cancellationToken)
            .ConfigureAwait(false);

        Assert.IsTrue(isUserNameAvailable);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationProvider_Register()
    {
        var document = GetUser();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = GetMockUserDao(document, cancellationToken);
        var noCancellation = CancellationToken.None;
        var mockPasswordDao = GetMockPasswordDao(noCancellation);
        var verificationToken = await TestUtilities.GenerateToken().ConfigureAwait(false);
        var mockVerificationDao = GetMockVerificationDao(verificationToken, noCancellation);
        var mockEmailer = GetMockEmailer(verificationToken, noCancellation);

        await RunRegisterTest(
            document,
            mockEmailer,
            mockPasswordDao,
            mockUserDao,
            mockVerificationDao,
            cancellationToken)
            .ConfigureAwait(false);

        mockEmailer.Verify();
        mockPasswordDao.Verify();
        mockUserDao.Verify();
        mockVerificationDao.Verify();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationProvider_Register_CancellationRequested_Ignored()
    {
        var document = GetUser();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = GetMockUserDao(document, cancellationToken);
        _ = mockUserDao.Setup(m => m.Save(document, cancellationToken))
            .Callback<UserDocument, CancellationToken>((u, t) => cancellationTokenSource.Cancel())
            .ReturnsAsync(new SaveResult() { Id = UserId });
        var noCancellation = CancellationToken.None;
        var mockPasswordDao = GetMockPasswordDao(noCancellation);
        var verificationToken = await TestUtilities.GenerateToken().ConfigureAwait(false);
        var mockVerificationDao = GetMockVerificationDao(verificationToken, noCancellation);
        var mockEmailer = GetMockEmailer(verificationToken, noCancellation);

        await RunRegisterTest(
            document,
            mockEmailer,
            mockPasswordDao,
            mockUserDao,
            mockVerificationDao,
            cancellationToken)
            .ConfigureAwait(false);

        mockEmailer.Verify();
        mockPasswordDao.Verify();
        mockUserDao.Verify();
        mockVerificationDao.Verify();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task RegistrationProvider_Register_ModelIsInvalid_Throws()
    {
        var model = new RegisterModel();
        var target = GetTarget();

        await target.Register(model, Action, RemoteIP).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task RegistrationProvider_Register_ModelIsNull_Throws()
    {
        var target = GetTarget();

        await target.Register(null!, Action, RemoteIP).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(RecaptchaVerificationFailedException))]
    [DataRow(RecaptchaVerificationResult.InsufficientScore)]
    [DataRow(RecaptchaVerificationResult.InvalidAction)]
    [DataRow(RecaptchaVerificationResult.InvalidHost)]
    [DataRow(RecaptchaVerificationResult.NotVerified)]
    public async Task RegistrationProvider_Register_RecaptchaVerificationFailed_Throws(
        RecaptchaVerificationResult result)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockRecaptchaValidator = GetMockRecaptchaResponseValidator(result, cancellationToken);
        var target = GetTarget(
            mockPasswordValidator: GetMockPasswordValidator(cancellationToken: cancellationToken),
            mockRecaptchaResponseValidator: mockRecaptchaValidator);

        await target.Register(GetRegisterModel(), Action, RemoteIP, cancellationToken)
            .ConfigureAwait(false);

        Assert.Fail(nameof(RecaptchaVerificationFailedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserAlreadyExistsException))]
    [DataRow(EmailAddress, null)]
    [DataRow(null, UserName)]
    public async Task RegistrationProvider_Register_UserAlreadyExistsException_Throws(
        string emailAddress,
        string userName)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockRecaptchaValidator = GetMockRecaptchaResponseValidator(cancellationToken: cancellationToken);
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        _ = mockUserDao.Setup(m => m.GetCount(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, t) =>
                {
                    var document = new UserDocument()
                    {
                        EmailAddress = EmailAddress,
                        UserName = UserName,
                    };

                    TestUtilities.AssertExpressionFilter(f, document);
                })
            .ReturnsAsync(1);
        var target = GetTarget(
            mockPasswordValidator: GetMockPasswordValidator(cancellationToken: cancellationToken),
            mockRecaptchaResponseValidator: mockRecaptchaValidator,
            mockUserDao: mockUserDao);
        var model = GetRegisterModel();
        model.EmailAddress = emailAddress;
        model.UserName = userName;

        await target.Register(model, Action, RemoteIP, cancellationToken)
            .ConfigureAwait(false);

        Assert.Fail(nameof(UserAlreadyExistsException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(OperationCanceledException))]
    public async Task RegistrationProvider_Register_UserDao_Fetch_CancellationRequested_Throws()
    {
        var cancellationToken = new CancellationToken(true);
        var mockRecaptchaClient = GetMockRecaptchaResponseValidator(cancellationToken: cancellationToken);
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        var document = new UserDocument() { UserName = UserName };
        _ = mockUserDao.Setup(m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, t) =>
                {
                    TestUtilities.AssertExpressionFilter(f, document);

                    if (t.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }
                })
            .ReturnsAsync(document);
        var target = GetTarget(mockRecaptchaResponseValidator: mockRecaptchaClient, mockUserDao: mockUserDao);

        await target.Register(GetRegisterModel(), Action, RemoteIP, cancellationToken)
            .ConfigureAwait(false);

        Assert.Fail(nameof(OperationCanceledException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(OperationCanceledException))]
    public async Task RegistrationProvider_Register_UserDao_Save_CancellationRequested_Throws()
    {
        var cancellationToken = new CancellationToken(true);
        var mockRecaptchaClient = GetMockRecaptchaResponseValidator(cancellationToken: cancellationToken);
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        var document = new UserDocument() { UserName = UserName };
        _ = mockUserDao.Setup(m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .ReturnsAsync(null as UserDocument);
        _ = mockUserDao.Setup(m => m.Save(It.IsAny<UserDocument>(), cancellationToken))
            .Callback<UserDocument, CancellationToken>(
                (f, t) =>
                {
                    if (t.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }
                })
            .ReturnsAsync(new SaveResult());
        var target = GetTarget(mockUserDao: mockUserDao);

        await target.Register(GetRegisterModel(), Action, RemoteIP, cancellationToken)
            .ConfigureAwait(false);

        Assert.Fail(nameof(OperationCanceledException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationProvider_RejectRegistration()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCachedValueDao = GetMockCachedValueDao(UserStatus.Suspended);
        var target = GetTarget(
            mockCachedValueDao: mockCachedValueDao,
            mockEmailTokenSerializer: GetMockEmailTokenSerializer(),
            mockUserDao: GetMockUserDao(GetUser(), cancellationToken),
            mockVerificationDao: GetMockVerificationDao(
                VerificationTokenVerificationResult.Verified, cancellationToken));

        await target.RejectRegistration(SerializedToken, cancellationToken).ConfigureAwait(false);

        mockCachedValueDao.Verify();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidTokenException))]
    [DataRow(VerificationTokenVerificationResult.NotVerified)]
    [DataRow(VerificationTokenVerificationResult.TokenExpired)]
    public async Task RegistrationProvider_RejectRegistration_InvalidToken_Throws(
        VerificationTokenVerificationResult verificationResult)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var target = GetTarget(
            mockEmailTokenSerializer: GetMockEmailTokenSerializer(),
            mockUserDao: GetMockUserDao(GetUser(), cancellationToken),
            mockVerificationDao: GetMockVerificationDao(verificationResult, cancellationToken));

        await target.RejectRegistration(SerializedToken, cancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task RegistrationProvider_RejectRegistration_UserDoesNotExist_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var target = GetTarget(mockEmailTokenSerializer: GetMockEmailTokenSerializer());

        await target.RejectRegistration(SerializedToken, cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserSuspendedException))]
    public async Task RegistrationProvider_RejectRegistration_UserSuspended_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = GetMockUserDao(GetUser(UserStatus.Suspended), cancellationToken);
        var target = GetTarget(mockEmailTokenSerializer: GetMockEmailTokenSerializer(), mockUserDao: mockUserDao);

        await target.RejectRegistration(SerializedToken, cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(UserSuspendedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationProvider_ResendVerificationEmail()
    {
        var isOnCooldown = false;

        var result = await RunResendVerificationEmailTest(
            GetUser(UserStatus.EmailNotVerified),
            isOnCooldown)
            .ConfigureAwait(false);

        Assert.AreEqual(ExpireTime, result.CooldownExpirationTime);
        Assert.AreEqual(TimeToLive, result.CooldownDuration);
        Assert.IsTrue(result.WasResent);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(EmailAddressAlreadyVerifiedException))]
    [DataRow(UserStatus.OK)]
    [DataRow(UserStatus.PasswordChangeRequired)]
    public async Task RegistrationProvider_ResendVerificationEmail_EmailAddressAlreadyVerified_Throws(
        UserStatus userStatus)
    {
        var document = GetUser(userStatus);

        _ = await RunResendVerificationEmailTest(document).ConfigureAwait(false);

        Assert.Fail(nameof(EmailAddressAlreadyVerifiedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationProvider_ResendVerificationEmail_IsOnCooldown()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockEmailer = GetMockEmailer(cancellationToken);

        var result = await RunResendVerificationEmailTest(
            GetUser(UserStatus.EmailNotVerified),
            true,
            mockEmailer,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        Assert.AreEqual(ExpireTime, result.CooldownExpirationTime);
        Assert.AreEqual(TimeToLive, result.CooldownDuration);
        Assert.IsFalse(result.WasResent);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task RegistrationProvider_ResendVerificationEmail_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        _ = await target.ResendVerificationEmail(new ResendVerificationEmailModel(), Action, RemoteIP)
            .ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task RegistrationProvider_ResendVerificationEmail_ModelIsNull_Throws()
    {
        var target = GetTarget();

        _ = await target.ResendVerificationEmail(null!, Action, RemoteIP)
            .ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(RecaptchaVerificationFailedException))]
    public async Task RegistrationProvider_ResendVerificationEmail_RecaptchaVerificationFailed_Throws()
    {
        var recaptchaResult = RecaptchaVerificationResult.NotVerified;

        _ = await RunResendVerificationEmailTest(recaptchaResult: recaptchaResult).ConfigureAwait(false);

        Assert.Fail(nameof(RecaptchaVerificationFailedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task RegistrationProvider_ResendVerificationEmail_UserDoesNotExist_Throws()
    {
        _ = await RunResendVerificationEmailTest(null!).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserSuspendedException))]
    public async Task RegistrationProvider_ResendVerificationEmail_UserSuspended_Throws()
    {
        var document = GetUser(UserStatus.Suspended);

        _ = await RunResendVerificationEmailTest(document).ConfigureAwait(false);

        Assert.Fail(nameof(UserSuspendedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegistrationProvider_ValidatePassword()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var target = GetTarget(mockPasswordValidator: GetMockPasswordValidator(
            cancellationToken: cancellationToken));

        var actual = await target.ValidatePassword(Password, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(ServicePasswordValidationResult.Valid, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(VerificationTokenVerificationResult.NotVerified, EmailVerificationResult.NotVerified)]
    [DataRow(VerificationTokenVerificationResult.TokenExpired, EmailVerificationResult.TokenExpired)]
    [DataRow(VerificationTokenVerificationResult.Verified, EmailVerificationResult.Verified)]
    public async Task RegistrationProvider_VerifyEmail(
        VerificationTokenVerificationResult tokenResult,
        EmailVerificationResult expectedResult)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = GetMockUserDao(GetUser(), cancellationToken);
        var mockVerificationDao = GetMockVerificationDao(tokenResult, cancellationToken);
        var target = GetTarget(
            mockEmailTokenSerializer: GetMockEmailTokenSerializer(),
            mockUserDao: mockUserDao,
            mockVerificationDao: mockVerificationDao);

        var actualResult = await target.VerifyEmaiAddress(SerializedToken, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task RegistrationProvider_VerifyEmail_UserDoesNotExist_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        _ = mockUserDao.Setup(m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, t) => TestUtilities.AssertExpressionFilter(f, GetUser()))
            .ReturnsAsync(null as UserDocument);
        var target = GetTarget(mockEmailTokenSerializer: GetMockEmailTokenSerializer(), mockUserDao: mockUserDao);

        _ = await target.VerifyEmaiAddress(SerializedToken, cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    private static Mock<ICacheClient> GetMockCache(bool isOnCooldown, CancellationToken cancellationToken = default)
    {
        var mockCache = new Mock<ICacheClient>();
        mockCache.Setup(m => m.GetCooldownStatus(EmailAddress, cancellationToken))
            .ReturnsAsync(new CooldownStatus(ExpireTime, isOnCooldown, TimeToLive))
            .Verifiable();

        return mockCache;
    }

    private static Mock<ICachedValueDao> GetMockCachedValueDao(UserStatus userStatus)
    {
        var mockDao = new Mock<ICachedValueDao>();
        mockDao.Setup(m => m.SetUserStatus(UserId, userStatus))
            .Returns(Task.CompletedTask)
            .Verifiable();

        return mockDao;
    }

    private static Mock<IEmailer> GetMockEmailer(
        VerificationToken verificationToken,
        CancellationToken cancellationToken)
    {
        var mockEmailer = new Mock<IEmailer>();
        mockEmailer.Setup(
            m => m.SendVerificationEmail(EmailAddress, Name, It.IsAny<EmailTokenData>(), cancellationToken))
            .Callback<string, string, EmailTokenData, CancellationToken>(
                (ea, n, etd, ct) =>
                {
                    Assert.AreEqual(verificationToken.Pepper, etd.Pepper);
                    Assert.AreEqual(verificationToken.Token, etd.Token);
                    Assert.AreEqual(UserName, etd.UserName);
                    Assert.AreEqual(MaxVerificationAge, etd.ValidityPeriod);
                    Assert.IsFalse(ct.IsCancellationRequested);
                })
            .Returns(Task.CompletedTask)
            .Verifiable();

        return mockEmailer;
    }

    private static Mock<IEmailer> GetMockEmailer(CancellationToken cancellationToken)
    {
        var mockEmailer = new Mock<IEmailer>();
        _ = mockEmailer.Setup(
            m => m.SendVerificationEmail(EmailAddress, Name, It.IsAny<EmailTokenData>(), cancellationToken))
            .Callback<string, string, EmailTokenData, CancellationToken>(
                (ea, n, etd, ct) =>
                {
                    Assert.AreEqual(Pepper, etd.Pepper);
                    Assert.AreEqual(Token, etd.Token);
                    Assert.AreEqual(UserName, etd.UserName);
                    Assert.AreEqual(MaxVerificationAge, etd.ValidityPeriod);
                    Assert.IsFalse(ct.IsCancellationRequested);
                })
            .Returns(Task.CompletedTask);

        return mockEmailer;
    }

    private static Mock<IEmailTokenSerializer> GetMockEmailTokenSerializer()
    {
        var mockSerializer = new Mock<IEmailTokenSerializer>();
        mockSerializer.Setup(m => m.Deserialize(SerializedToken))
            .Returns(new Tuple<string, string, string>(Pepper, Token, UserName))
            .Verifiable();

        return mockSerializer;
    }

    private static Mock<IMapper> GetMockMapper(RegisterModel model, UserDocument document)
    {
        var mockMapper = new Mock<IMapper>();
        _ = mockMapper.Setup(m => m.Map<UserDocument>(model))
            .Returns(document);

        return mockMapper;
    }

    private static Mock<IPasswordDao> GetMockPasswordDao(CancellationToken cancellationToken)
    {
        var mockPasswordDao = new Mock<IPasswordDao>();
        mockPasswordDao.Setup(m => m.SavePassword(UserId, Password, cancellationToken))
            .Callback<string, string, CancellationToken>((id, p, ct) => Assert.IsFalse(ct.IsCancellationRequested))
            .Returns(Task.CompletedTask)
            .Verifiable();

        return mockPasswordDao;
    }

    private static Mock<IPasswordValidator> GetMockPasswordValidator(
        CommonPasswordValidationResult result = CommonPasswordValidationResult.Valid,
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
        UserDocument user,
        CancellationToken cancellationToken)
    {
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        var document = GetUser();

        _ = mockUserDao.Setup(
            m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, ct) => TestUtilities.AssertExpressionFilter(f, document))
            .ReturnsAsync(user);
        mockUserDao.Setup(m => m.GetCount(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, ct) => TestUtilities.AssertExpressionFilter(f, document))
            .ReturnsAsync(0)
            .Verifiable();
        mockUserDao.Setup(m => m.Save(user, cancellationToken))
            .ReturnsAsync(new SaveResult() { Id = UserId })
            .Verifiable();

        return mockUserDao;
    }

    private static Mock<IVerificationDao> GetMockVerificationDao(
        VerificationTokenVerificationResult verificationResult,
        CancellationToken cancellationToken = default)
    {
        var mockVerificationDao = new Mock<IVerificationDao>();
        mockVerificationDao.Setup(m => m.VerifyToken(
            UserId,
            EmailAddress,
            VerificationType.EmailVerification,
            Token,
            Pepper,
            cancellationToken))
            .ReturnsAsync(verificationResult)
            .Verifiable();

        return mockVerificationDao;
    }

    private static Mock<IVerificationDao> GetMockVerificationDao(
        VerificationToken verificationToken,
        CancellationToken cancellationToken)
    {
        var mockVerificationDao = new Mock<IVerificationDao>();
        mockVerificationDao.Setup(
            m => m.GenerateVerificationToken(
                UserId,
                EmailAddress,
                VerificationType.EmailVerification,
                cancellationToken))
            .Callback<string, string, VerificationType, CancellationToken>(
                (id, ea, vt, ct) => Assert.IsFalse(ct.IsCancellationRequested))
            .ReturnsAsync(verificationToken)
            .Verifiable();

        return mockVerificationDao;
    }

    private static Mock<IOptions<VerificationOptions>> GetMockVerificationOptions()
    {
        var mockOptions = new Mock<IOptions<VerificationOptions>>();
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new VerificationOptions()
            {
                HashName = HashAlgorithmName.SHA512,
                MaxVerificationAge = MaxVerificationAge,
                MinVerificationAge = default,
                PepperLength = PepperLength,
                SaltLength = default,
            });

        return mockOptions;
    }

    private static RegisterModel GetRegisterModel()
    {
        return new RegisterModel()
        {
            EmailAddress = EmailAddress,
            Name = Name,
            Password = Password,
            RecaptchaResponse = RecaptchaResponse,
            UserName = UserName,
        };
    }

    private static ResendVerificationEmailModel GetResendVerificationEmailModel()
    {
        return new ResendVerificationEmailModel()
        {
            EmailAddress = EmailAddress,
            RecaptchaResponse = RecaptchaResponse,
        };
    }

    private static RegistrationProvider GetTarget(
        Mock<ICacheClient>? mockCache = default,
        Mock<ICachedValueDao>? mockCachedValueDao = default,
        Mock<IEmailer>? mockEmailer = default,
        Mock<IEmailTokenSerializer>? mockEmailTokenSerializer = default,
        Mock<IMapper>? mockMapper = default,
        Mock<IPasswordDao>? mockPasswordDao = default,
        Mock<IPasswordValidator>? mockPasswordValidator = default,
        Mock<IRecaptchaResponseValidator>? mockRecaptchaResponseValidator = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default,
        Mock<IVerificationDao>? mockVerificationDao = default,
        Mock<IOptions<VerificationOptions>>? mockVerificationOptions = default)
    {
        var mapper = mockMapper?.Object
            ?? new MapperConfiguration(c => c.AddProfile<RegistrationProfile>()).CreateMapper();
        var passwordValidator = mockPasswordValidator?.Object ?? Mock.Of<IPasswordValidator>();

        return new RegistrationProvider(
            mockCache?.Object ?? Mock.Of<ICacheClient>(),
            mockCachedValueDao?.Object ?? Mock.Of<ICachedValueDao>(),
            mockEmailer?.Object ?? Mock.Of<IEmailer>(),
            mockEmailTokenSerializer?.Object ?? Mock.Of<IEmailTokenSerializer>(),
            mapper,
            mockPasswordDao?.Object ?? Mock.Of<IPasswordDao>(),
            passwordValidator,
            mockRecaptchaResponseValidator?.Object ?? Mock.Of<IRecaptchaResponseValidator>(),
            new RegisterModelValidator(passwordValidator),
            new ResendVerificationEmailModelValidator(),
            mockUserDao?.Object ?? Mock.Of<IDocumentDao<UserDocument>>(),
            mockVerificationDao?.Object ?? Mock.Of<IVerificationDao>(),
            mockVerificationOptions?.Object ?? Mock.Of<IOptions<VerificationOptions>>());
    }

    private static UserDocument GetUser(UserStatus userStatus = UserStatus.OK)
    {
        return new UserDocument()
        {
            EmailAddress = EmailAddress,
            Id = UserId,
            UserName = UserName,
            UserStatus = userStatus,
        };
    }

    private static async Task RunRegisterTest(
        UserDocument document,
        Mock<IEmailer> mockEmailer,
        Mock<IPasswordDao> mockPasswordDao,
        Mock<IDocumentDao<UserDocument>> mockUserDao,
        Mock<IVerificationDao> mockVerificationDao,
        CancellationToken cancellationToken)
    {
        var mockCache = GetMockCache(false, cancellationToken);
        var model = new RegisterModel()
        {
            EmailAddress = EmailAddress,
            Name = Name,
            Password = Password,
            RecaptchaResponse = RecaptchaResponse,
            UserName = UserName,
        };
        var mockMapper = GetMockMapper(model, document);
        var verificationToken = await TestUtilities.GenerateToken().ConfigureAwait(false);
        mockEmailer ??= GetMockEmailer(verificationToken, cancellationToken);
        mockPasswordDao ??= GetMockPasswordDao(cancellationToken);
        mockUserDao ??= GetMockUserDao(document, cancellationToken);
        mockVerificationDao ??= GetMockVerificationDao(verificationToken, cancellationToken);

        var target = GetTarget(
            mockCache: mockCache,
            mockEmailer: mockEmailer,
            mockMapper: mockMapper,
            mockPasswordDao: mockPasswordDao,
            mockPasswordValidator: GetMockPasswordValidator(cancellationToken: cancellationToken),
            mockRecaptchaResponseValidator: GetMockRecaptchaResponseValidator(cancellationToken: cancellationToken),
            mockUserDao: mockUserDao,
            mockVerificationDao: mockVerificationDao,
            mockVerificationOptions: GetMockVerificationOptions());

        await target.Register(model, Action, RemoteIP, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ResendVerificationEmailResult> RunResendVerificationEmailTest(
        UserDocument? document = default,
        bool isOnCooldown = false,
        Mock<IEmailer>? mockEmailer = default,
        RecaptchaVerificationResult recaptchaResult = RecaptchaVerificationResult.Verified,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default)
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
        }

        var mockCache = GetMockCache(isOnCooldown, cancellationToken);
        var mockRecaptchaResponseValidator = GetMockRecaptchaResponseValidator(
            recaptchaResult,
            cancellationToken: cancellationToken);
        var mockUserDao = GetMockUserDao(document!, cancellationToken);
        var verificationToken = await TestUtilities.GenerateToken().ConfigureAwait(false);
        var mockVerificationDao = GetMockVerificationDao(verificationToken, cancellationToken);
        var target = GetTarget(
            mockCache: mockCache,
            mockEmailer: mockEmailer,
            mockRecaptchaResponseValidator: mockRecaptchaResponseValidator,
            mockUserDao: mockUserDao,
            mockVerificationDao: mockVerificationDao,
            mockVerificationOptions: GetMockVerificationOptions());

        return await target.ResendVerificationEmail(
            GetResendVerificationEmailModel(),
            Action,
            RemoteIP,
            cancellationToken)
            .ConfigureAwait(false);
    }
}
