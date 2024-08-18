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

using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.TestTools;
using Moq;
using System.Linq.Expressions;

[TestClass]
public class RejectRegistrationHandlerTests
{
    private const string EmailAddress = "test@test.local";
    private const string Pepper = TestData.Pepper;
    private const string SerializedToken = TestData.SerializedToken;
    private const string Token = TestData.Token;
    private const string UserId = nameof(UserId);
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RejectRegistrationHandler_Constructor()
    {
        var target = new RejectRegistrationHandler(
            Mock.Of<ICachedValueDao>(),
            Mock.Of<IEmailTokenSerializer>(),
            Mock.Of<IDocumentDao<UserDocument>>(),
            Mock.Of<IVerificationDao>());

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RejectRegistrationHandler_RejectRegistration()
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
    public async Task RejectRegistrationHandler_RejectRegistration_InvalidToken_Throws(
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
    public async Task RejectRegistrationHandler_RejectRegistration_UserDoesNotExist_Throws()
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
    public async Task RejectRegistrationHandler_RejectRegistration_UserSuspended_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = GetMockUserDao(GetUser(UserStatus.Suspended), cancellationToken);
        var target = GetTarget(mockEmailTokenSerializer: GetMockEmailTokenSerializer(), mockUserDao: mockUserDao);

        await target.RejectRegistration(SerializedToken, cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(UserSuspendedException) + " expected");
    }

    private static Mock<ICachedValueDao> GetMockCachedValueDao(UserStatus userStatus)
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

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDao(
        UserDocument user,
        CancellationToken cancellationToken)
    {
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();

        _ = mockUserDao.Setup(
            m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, ct) => TestUtilities.AssertExpressionFilter(f, user))
            .ReturnsAsync(user);

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

    private static RejectRegistrationHandler GetTarget(
        Mock<ICachedValueDao>? mockCachedValueDao = default,
        Mock<IEmailTokenSerializer>? mockEmailTokenSerializer = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default,
        Mock<IVerificationDao>? mockVerificationDao = default)
    {
        return new RejectRegistrationHandler(
            mockCachedValueDao?.Object ?? Mock.Of<ICachedValueDao>(),
            mockEmailTokenSerializer?.Object ?? Mock.Of<IEmailTokenSerializer>(),
            mockUserDao?.Object ?? Mock.Of<IDocumentDao<UserDocument>>(),
            mockVerificationDao?.Object ?? Mock.Of<IVerificationDao>());
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
}
