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

[TestClass]
public class ResendVerificationEmailHandlerTests
{
    private const string Action = nameof(Action);
    private const string EmailAddress = "test@test.local";
    private const string Name = nameof(Name);
    private const string Pepper = TestData.Pepper;
    private const int PepperLength = 32;
    private const string RecaptchaResponse = nameof(RecaptchaResponse);
    private const string Token = TestData.Token;
    private const string RemoteIP = "127.0.0.1";
    private const string UserId = nameof(UserId);
    private const string UserName = nameof(UserName);
    private static readonly DateTime? ExpireTime = DateTime.UtcNow + TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxVerificationAge = TimeSpan.FromDays(1);
    private static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(5);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResendVerificationEmailHandler_Constructor()
    {
        var target = new ResendVerificationEmailHandler(
            Mock.Of<ICacheClient>(),
            Mock.Of<IEmailer>(),
            Mock.Of<IRecaptchaResponseValidator>(),
            Mock.Of<IValidator<ResendVerificationEmailModel>>(),
            Mock.Of<IDocumentDao<UserDocument>>(),
            Mock.Of<IVerificationDao>(),
            Mock.Of<IOptions<VerificationOptions>>());

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task ResendVerificationEmailHandler_ResendVerificationEmail()
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
    public async Task ResendVerificationEmailHandler_ResendVerificationEmail_EmailAddressAlreadyVerified_Throws(
        UserStatus userStatus)
    {
        var document = GetUser(userStatus);

        _ = await RunResendVerificationEmailTest(document).ConfigureAwait(false);

        Assert.Fail(nameof(EmailAddressAlreadyVerifiedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task ResendVerificationEmailHandler_ResendVerificationEmail_IsOnCooldown()
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
    public async Task ResendVerificationEmailHandler_ResendVerificationEmail_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        _ = await target.ResendVerificationEmail(new ResendVerificationEmailModel(), Action, RemoteIP)
            .ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task ResendVerificationEmailHandler_ResendVerificationEmail_ModelIsNull_Throws()
    {
        var target = GetTarget();

        _ = await target.ResendVerificationEmail(null!, Action, RemoteIP)
            .ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(RecaptchaVerificationFailedException))]
    public async Task ResendVerificationEmailHandler_ResendVerificationEmail_RecaptchaVerificationFailed_Throws()
    {
        var recaptchaResult = RecaptchaVerificationResult.NotVerified;

        _ = await RunResendVerificationEmailTest(recaptchaResult: recaptchaResult).ConfigureAwait(false);

        Assert.Fail(nameof(RecaptchaVerificationFailedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task ResendVerificationEmailHandler_ResendVerificationEmail_UserDoesNotExist_Throws()
    {
        _ = await RunResendVerificationEmailTest(null!).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserSuspendedException))]
    public async Task ResendVerificationEmailHandler_ResendVerificationEmail_UserSuspended_Throws()
    {
        var document = GetUser(UserStatus.Suspended);

        _ = await RunResendVerificationEmailTest(document).ConfigureAwait(false);

        Assert.Fail(nameof(UserSuspendedException) + " expected");
    }

    private static Mock<ICacheClient> GetMockCache(bool isOnCooldown, CancellationToken cancellationToken = default)
    {
        var mockCache = new Mock<ICacheClient>();
        mockCache.Setup(m => m.GetCooldownStatus(EmailAddress, cancellationToken))
            .ReturnsAsync(new CooldownStatus(ExpireTime, isOnCooldown, TimeToLive))
            .Verifiable();

        return mockCache;
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

        _ = mockUserDao.Setup(
            m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, ct) => TestUtilities.AssertExpressionFilter(f, GetUser()))
            .ReturnsAsync(user);

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

    private static ResendVerificationEmailModel GetResendVerificationEmailModel()
    {
        return new ResendVerificationEmailModel()
        {
            EmailAddress = EmailAddress,
            RecaptchaResponse = RecaptchaResponse,
        };
    }

    private static ResendVerificationEmailHandler GetTarget(
        Mock<ICacheClient>? mockCache = default,
        Mock<IEmailer>? mockEmailer = default,
        Mock<IMapper>? mockMapper = default,
        Mock<IPasswordValidator>? mockPasswordValidator = default,
        Mock<IRecaptchaResponseValidator>? mockRecaptchaResponseValidator = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default,
        Mock<IVerificationDao>? mockVerificationDao = default,
        Mock<IOptions<VerificationOptions>>? mockVerificationOptions = default)
    {
        var mapper = mockMapper?.Object
            ?? new MapperConfiguration(c => c.AddProfile<RegistrationProfile>()).CreateMapper();
        var passwordValidator = mockPasswordValidator?.Object ?? Mock.Of<IPasswordValidator>();

        return new ResendVerificationEmailHandler(
            mockCache?.Object ?? Mock.Of<ICacheClient>(),
            mockEmailer?.Object ?? Mock.Of<IEmailer>(),
            mockRecaptchaResponseValidator?.Object ?? Mock.Of<IRecaptchaResponseValidator>(),
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
