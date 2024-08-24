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
using Hirameku.Email;
using Hirameku.TestTools;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;

[TestClass]
public class UpdateEmailAddressHandlerTests
{
    private const string EmailAddress = "test@test.local";
    private const string Name = nameof(Name);
    private const string UserId = "1234567890abcdef12345678";
    private const string UserName = nameof(UserName);
    private static readonly TimeSpan MaxVerificationAge = TimeSpan.FromDays(1);
    private static readonly DateTime Now = DateTime.UtcNow;

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UpdateEmailAddressHandler_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public Task UpdateEmailAddressHandler_UpdateEmailAddress()
    {
        return RunUpdateEmailAddressTest(GetUser(), UserStatus.EmailNotVerified);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task UpdateEmailAddressHandler_UpdateEmailAddress_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        await target.UpdateEmailAddress(
            new Authenticated<UpdateEmailAddressModel>(
                new UpdateEmailAddressModel(),
                GetClaimsPrincipal()))
            .ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public Task UpdateEmailAddressHandler_UpdateEmailAddress_PasswordChangeRequired()
    {
        var user = GetUser();
        user.UserStatus = UserStatus.PasswordChangeRequired;

        return RunUpdateEmailAddressTest(user, UserStatus.EmailNotVerifiedAndPasswordChangeRequired);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task UpdateEmailAddressHandler_UpdateEmailAddress_UserDoesNotExist_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var target = GetTarget(mockUserDao: GetMockUserDao(cancellationToken: cancellationToken));

        await target.UpdateEmailAddress(
            new Authenticated<UpdateEmailAddressModel>(
                new UpdateEmailAddressModel() { EmailAddress = EmailAddress },
                GetClaimsPrincipal()),
            cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    private static ClaimsPrincipal GetClaimsPrincipal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, UserId) }));
    }

    private static Mock<ICachedValueDao> GetMockCachedValueDao(UserStatus userStatus)
    {
        var mockDao = new Mock<ICachedValueDao>();
        mockDao.Setup(m => m.SetUserStatus(UserId, userStatus))
            .Returns(Task.CompletedTask)
            .Verifiable();

        return mockDao;
    }

    private static Mock<IEmailer> GetMockEmailer(VerificationToken verificationToken)
    {
        var mockEmailer = new Mock<IEmailer>();
        mockEmailer.Setup(
            m => m.SendVerificationEmail(EmailAddress, Name, It.IsAny<EmailTokenData>(), CancellationToken.None))
            .Callback<string, string, EmailTokenData, CancellationToken>(
                (ea, n, td, ct) =>
                {
                    Assert.AreEqual(verificationToken.Pepper, td.Pepper);
                    Assert.AreEqual(verificationToken.Token, td.Token);
                    Assert.AreEqual(UserName, td.UserName);
                    Assert.AreEqual(MaxVerificationAge, td.ValidityPeriod);
                })
            .Returns(Task.CompletedTask)
            .Verifiable();

        return mockEmailer;
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

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDaoForUpdateEmailAddress(
        UserDocument? user = default,
        CancellationToken cancellationToken = default)
    {
        var mockDao = GetMockUserDao(user, cancellationToken);
        mockDao.Setup(
            m => m.Update(UserId, It.IsAny<Expression<Func<UserDocument, string>>>(), EmailAddress, cancellationToken))
            .Callback<string, Expression<Func<UserDocument, string>>, string, CancellationToken>(
                (id, f, v, _) =>
                {
                    Assert.AreEqual(UserId, id);
                    TestUtilities.AssertMemberExpression(f, nameof(UserDocument.EmailAddress));
                    Assert.AreEqual(EmailAddress, v);
                })
            .Returns(Task.CompletedTask)
            .Verifiable();

        return mockDao;
    }

    private static Mock<IVerificationDao> GetMockVerificationDao(VerificationToken verificationToken)
    {
        var mockDao = new Mock<IVerificationDao>();
        mockDao.Setup(
            m => m.GenerateVerificationToken(
                UserId,
                EmailAddress,
                VerificationType.EmailVerification,
                CancellationToken.None))
            .ReturnsAsync(verificationToken)
            .Verifiable();

        return mockDao;
    }

    private static Mock<IOptions<VerificationOptions>> GetMockVerificationOptions()
    {
        var mockOptions = new Mock<IOptions<VerificationOptions>>();
        mockOptions.Setup(m => m.Value)
            .Returns(new VerificationOptions() { MaxVerificationAge = MaxVerificationAge })
            .Verifiable();

        return mockOptions;
    }

    private static UpdateEmailAddressHandler GetTarget(
        Mock<ICachedValueDao>? mockCachedValueDao = default,
        Mock<IEmailer>? mockEmailer = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default,
        Mock<IVerificationDao>? mockVerificationDao = default,
        Mock<IOptions<VerificationOptions>>? mockVerificationOptions = default)
    {
        return new UpdateEmailAddressHandler(
            mockCachedValueDao?.Object ?? Mock.Of<ICachedValueDao>(),
            mockEmailer?.Object ?? Mock.Of<IEmailer>(),
            new AuthenticatedValidator<UpdateEmailAddressModel>(new UpdateEmailAddressModelValidator()),
            mockUserDao?.Object ?? Mock.Of<IDocumentDao<UserDocument>>(),
            mockVerificationDao?.Object ?? Mock.Of<IVerificationDao>(),
            mockVerificationOptions?.Object ?? Mock.Of<IOptions<VerificationOptions>>());
    }

    private static UserDocument GetUser(UserStatus userStatus = UserStatus.OK)
    {
        return new UserDocument()
        {
            Id = UserId,
            Name = Name,
            UserName = UserName,
            UserStatus = userStatus,
        };
    }

    private static async Task RunUpdateEmailAddressTest(UserDocument user, UserStatus newUserStatus)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCachedValueDao = GetMockCachedValueDao(newUserStatus);
        var verificationToken = await TestUtilities.GenerateToken(Now, EmailAddress, Now + MaxVerificationAge)
            .ConfigureAwait(false);
        var mockEmailer = GetMockEmailer(verificationToken);
        var mockUserDao = GetMockUserDaoForUpdateEmailAddress(user, cancellationToken);
        var mockVerificationDao = GetMockVerificationDao(verificationToken);
        var mockVerificationOptions = GetMockVerificationOptions();
        var target = GetTarget(
            mockCachedValueDao,
            mockEmailer,
            mockUserDao: mockUserDao,
            mockVerificationDao: mockVerificationDao,
            mockVerificationOptions: mockVerificationOptions);

        await target.UpdateEmailAddress(
            new Authenticated<UpdateEmailAddressModel>(
                new UpdateEmailAddressModel() { EmailAddress = EmailAddress },
                GetClaimsPrincipal()),
            cancellationToken)
            .ConfigureAwait(false);

        mockCachedValueDao.Verify();
        mockEmailer.Verify();
        mockUserDao.Verify();
        mockVerificationDao.Verify();
        mockVerificationOptions.Verify();
    }
}
