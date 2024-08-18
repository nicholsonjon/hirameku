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

using FluentValidation;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Hirameku.TestTools;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq.Expressions;

[TestClass]
public class SendPasswordResetTests
{
    private const string Action = nameof(Action);
    private const string EmailAddress = nameof(EmailAddress);
    private const string Name = nameof(Name);
    private const string Pepper = TestData.Pepper;
    private const string RecaptchaResponse = nameof(RecaptchaResponse);
    private const string RemoteIP = "127.0.0.1";
    private const string Token = TestData.Token;
    private const string UserId = "1234567890abcdef12345678";
    private const string UserName = nameof(UserName);
    private static readonly TimeSpan MaxVerificationAge = TimeSpan.FromDays(1);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SendPasswordResetHandler_Constructor()
    {
        var target = new SendPasswordResetHandler(
            Mock.Of<IEmailer>(),
            Mock.Of<IRecaptchaResponseValidator>(),
            Mock.Of<IValidator<SendPasswordResetModel>>(),
            Mock.Of<IDocumentDao<UserDocument>>(),
            Mock.Of<IVerificationDao>(),
            Mock.Of<IOptions<VerificationOptions>>());

        Assert.IsNotNull(target);
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

    private static Mock<IOptions<VerificationOptions>> GetMockVerificationOptions()
    {
        var mockOptions = new Mock<IOptions<VerificationOptions>>();
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new VerificationOptions() { MaxVerificationAge = MaxVerificationAge });

        return mockOptions;
    }

    private static SendPasswordResetModel GetSendPasswordResetModel()
    {
        return new SendPasswordResetModel()
        {
            RecaptchaResponse = RecaptchaResponse,
            UserName = UserName,
        };
    }

    private static SendPasswordResetHandler GetTarget(
        Mock<IEmailer>? mockEmailer = default,
        Mock<IRecaptchaResponseValidator>? mockRecaptchaResponseValidator = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default,
        Mock<IVerificationDao>? mockVerificationDao = default)
    {
        return new SendPasswordResetHandler(
            mockEmailer?.Object ?? Mock.Of<IEmailer>(),
            mockRecaptchaResponseValidator?.Object ?? Mock.Of<IRecaptchaResponseValidator>(),
            new SendPasswordResetModelValidator(),
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
