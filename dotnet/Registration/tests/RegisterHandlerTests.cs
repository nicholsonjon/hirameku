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

[TestClass]
public class RegisterHandlerTests
{
    private const string Action = nameof(Action);
    private const string EmailAddress = "test@test.local";
    private const string Name = nameof(Name);
    private const string Password = TestData.Password;
    private const int PepperLength = 32;
    private const string RecaptchaResponse = nameof(RecaptchaResponse);
    private const string RemoteIP = "127.0.0.1";
    private const string UserId = nameof(UserId);
    private const string UserName = nameof(UserName);
    private static readonly DateTime? ExpireTime = DateTime.UtcNow + TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxVerificationAge = TimeSpan.FromDays(1);
    private static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(5);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegisterHandler_Constructor()
    {
        var target = new RegisterHandler(
            Mock.Of<ICacheClient>(),
            Mock.Of<IEmailer>(),
            Mock.Of<IMapper>(),
            Mock.Of<IPasswordDao>(),
            Mock.Of<IRecaptchaResponseValidator>(),
            Mock.Of<IValidator<RegisterModel>>(),
            Mock.Of<IDocumentDao<UserDocument>>(),
            Mock.Of<IVerificationDao>(),
            Mock.Of<IOptions<VerificationOptions>>());

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegisterHandler_Register()
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
    public async Task RegisterHandler_Register_CancellationRequested_Ignored()
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
    public async Task RegisterHandler_Register_ModelIsInvalid_Throws()
    {
        var model = new RegisterModel();
        var target = GetTarget();

        await target.Register(model, Action, RemoteIP).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task RegisterHandler_Register_ModelIsNull_Throws()
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
    public async Task RegisterHandler_Register_RecaptchaVerificationFailed_Throws(
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
    public async Task RegisterHandler_Register_UserAlreadyExistsException_Throws(
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
    public async Task RegisterHandler_Register_UserDao_Fetch_CancellationRequested_Throws()
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
                    t.ThrowIfCancellationRequested();
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
    public async Task RegisterHandler_Register_UserDao_Save_CancellationRequested_Throws()
    {
        var cancellationToken = new CancellationToken(true);
        var mockRecaptchaClient = GetMockRecaptchaResponseValidator(cancellationToken: cancellationToken);
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        var document = new UserDocument() { UserName = UserName };
        _ = mockUserDao.Setup(m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .ReturnsAsync(null as UserDocument);
        _ = mockUserDao.Setup(m => m.Save(It.IsAny<UserDocument>(), cancellationToken))
            .Callback<UserDocument, CancellationToken>((f, t) => t.ThrowIfCancellationRequested())
            .ReturnsAsync(new SaveResult());
        var target = GetTarget(mockUserDao: mockUserDao);

        await target.Register(GetRegisterModel(), Action, RemoteIP, cancellationToken)
            .ConfigureAwait(false);

        Assert.Fail(nameof(OperationCanceledException) + " expected");
    }

    private static Mock<ICacheClient> GetMockCache(bool isOnCooldown, CancellationToken cancellationToken = default)
    {
        var mockCache = new Mock<ICacheClient>();
        mockCache.Setup(m => m.GetCooldownStatus(EmailAddress, cancellationToken))
            .ReturnsAsync(new CooldownStatus(ExpireTime, isOnCooldown, TimeToLive))
            .Verifiable();

        return mockCache;
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

        mockUserDao.Setup(m => m.GetCount(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, ct) => TestUtilities.AssertExpressionFilter(f, user))
            .ReturnsAsync(0)
            .Verifiable();
        mockUserDao.Setup(m => m.Save(user, cancellationToken))
            .ReturnsAsync(new SaveResult() { Id = UserId })
            .Verifiable();

        return mockUserDao;
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

    private static RegisterHandler GetTarget(
        Mock<ICacheClient>? mockCache = default,
        Mock<IEmailer>? mockEmailer = default,
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

        return new RegisterHandler(
            mockCache?.Object ?? Mock.Of<ICacheClient>(),
            mockEmailer?.Object ?? Mock.Of<IEmailer>(),
            mapper,
            mockPasswordDao?.Object ?? Mock.Of<IPasswordDao>(),
            mockRecaptchaResponseValidator?.Object ?? Mock.Of<IRecaptchaResponseValidator>(),
            new RegisterModelValidator(passwordValidator),
            mockUserDao?.Object ?? Mock.Of<IDocumentDao<UserDocument>>(),
            mockVerificationDao?.Object ?? Mock.Of<IVerificationDao>(),
            mockVerificationOptions?.Object ?? Mock.Of<IOptions<VerificationOptions>>());
    }

    private static UserDocument GetUser()
    {
        return new UserDocument()
        {
            EmailAddress = EmailAddress,
            Id = UserId,
            UserName = UserName,
            UserStatus = UserStatus.OK,
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
}
