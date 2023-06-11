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

namespace Hirameku.Data.Tests;

using Hirameku.Common;
using Hirameku.Data;
using Hirameku.TestTools;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;

[TestClass]
public class VerificationDaoTests
{
    private const string EmailAddress = nameof(EmailAddress);
    private const string Name = nameof(Name);
    private const int SaltAndPepperLength = 16;
    private const string UserId = nameof(UserId);
    private static readonly HashAlgorithmName HashName = HashAlgorithmName.SHA256;
    private static readonly TimeSpan MaxVerificationAge = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan MinVerificationAge = TimeSpan.FromMinutes(1);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void VerificationDao_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(VerificationDao_GenerateVerificationToken_EmailAddress_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(VerificationDao_GenerateVerificationToken_EmailAddress_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(VerificationDao_GenerateVerificationToken_EmailAddress_Throws) + "(WhiteSpace)")]
    public async Task VerificationDao_GenerateVerificationToken_EmailAddress_Throws(string emailAddress)
    {
        var target = GetTarget();

        _ = await target.GenerateVerificationToken(UserId, emailAddress, default)
            .ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task VerificationDao_GenerateVerificationToken_ExpirePriorVerification()
    {
        var now = DateTime.UtcNow;
        var verification = GetVerification(now);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCollection = GetMockVerificationCollection(now, verification, cancellationToken: cancellationToken);
        Expression<Func<IMongoCollection<Verification>, Task<UpdateResult>>> updateOneAsync =
            m => m.UpdateOneAsync(
                It.IsAny<FilterDefinition<Verification>>(),
                It.IsAny<UpdateDefinition<Verification>>(),
                default,
                cancellationToken);
        _ = mockCollection.Setup(updateOneAsync)
            .Callback<FilterDefinition<Verification>, UpdateDefinition<Verification>, UpdateOptions, CancellationToken>(
                (f, u, o, c) =>
                {
                    TestUtilities.AssertUpdate<Verification, DateTime?>(
                        f,
                        u,
                        verification,
                        nameof(Verification.ExpirationDate),
                        now);
                })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, default));

        var verificationToken = await RunGenerateVerificationTokenTest(
            now,
            MaxVerificationAge,
            verification: verification,
            mockVerificationCollection: mockCollection,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        AssertGenerateVerificationTokenTest(now + MaxVerificationAge, verificationToken);
        mockCollection.Verify(updateOneAsync);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task VerificationDao_GenerateVerificationToken_ExpirePriorVerification_VerificationAlreadyExpired()
    {
        var creationDate = DateTime.UtcNow;
        var verification = GetVerification(creationDate);
        verification.ExpirationDate = creationDate;

        var verificationToken = await RunGenerateVerificationTokenTest(
            creationDate,
            MaxVerificationAge,
            verification: verification,
            assertFilter: false)
            .ConfigureAwait(false);

        AssertGenerateVerificationTokenTest(creationDate + MaxVerificationAge, verificationToken);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(VerificationException))]
    public async Task VerificationDao_GenerateVerificationToken_ExpirePriorVerification_VerificationTooRecent()
    {
        var now = DateTime.UtcNow;
        var verification = GetVerification(now);
        var creationDate = verification.CreationDate = now;

        _ = await RunGenerateVerificationTokenTest(creationDate, MaxVerificationAge, verification: verification)
            .ConfigureAwait(false);

        Assert.Fail(nameof(VerificationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task VerificationDao_GenerateVerificationToken_NoExpirationDate()
    {
        var creationDate = DateTime.UtcNow;

        var verificationToken = await RunGenerateVerificationTokenTest(creationDate, maxVerificationAge: null)
            .ConfigureAwait(false);

        AssertGenerateVerificationTokenTest(null, verificationToken);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task VerificationDao_GenerateVerificationToken()
    {
        var creationDate = DateTime.UtcNow;

        var verificationToken = await RunGenerateVerificationTokenTest(
            creationDate,
            MaxVerificationAge,
            assertFilter: true)
            .ConfigureAwait(false);

        AssertGenerateVerificationTokenTest(creationDate + MaxVerificationAge, verificationToken);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    public async Task VerificationDao_GenerateVerificationToken_TypeIsInvalid_Throws()
    {
        var target = GetTarget();

        _ = await target.GenerateVerificationToken(UserId, EmailAddress, (VerificationType)(-1))
            .ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(VerificationDao_GenerateVerificationToken_UserId_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(VerificationDao_GenerateVerificationToken_UserId_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(VerificationDao_GenerateVerificationToken_UserId_Throws) + "(WhiteSpace)")]
    public async Task VerificationDao_GenerateVerificationToken_UserId_Throws(string userId)
    {
        var target = GetTarget();

        _ = await target.GenerateVerificationToken(userId, EmailAddress, default)
            .ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task VerificationDao_VerifyToken_DoesNotExist_Throws()
    {
        var result = await RunVerifyTokenTest(
            DateTime.UtcNow,
            TimeSpan.Zero,
            verification: null,
            assertFilter: false)
            .ConfigureAwait(false);

        Assert.AreEqual(VerificationTokenVerificationResult.NotVerified, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task VerificationDao_VerifyToken_Expired()
    {
        var now = DateTime.UtcNow;
        var verification = GetVerification(now);
        verification.ExpirationDate = now;

        var result = await RunVerifyTokenTest(now, TimeSpan.Zero, verification: verification, assertFilter: false)
            .ConfigureAwait(false);

        Assert.AreEqual(VerificationTokenVerificationResult.TokenExpired, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(1, "", "", "")]
    [DataRow(0, "1", "", "")]
    [DataRow(0, "", "1234", "")]
    [DataRow(0, "", "", "1234")]
    public async Task VerificationDao_VerifyToken_NotVerified(
        int creationDateOffset,
        string emailAddressOffset,
        string pepperOffset,
        string tokenOffset)
    {
        var result = await RunVerifyTokenTest(
            DateTime.UtcNow,
            new TimeSpan(creationDateOffset),
            emailAddressOffset,
            pepperOffset,
            tokenOffset).ConfigureAwait(false);

        Assert.AreEqual(VerificationTokenVerificationResult.NotVerified, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(VerificationType.EmailVerification, UserStatus.EmailNotVerified, UserStatus.OK)]
    [DataRow(VerificationType.EmailVerification, UserStatus.EmailNotVerifiedAndPasswordChangeRequired, UserStatus.PasswordChangeRequired)]
    [DataRow(VerificationType.PasswordReset, UserStatus.EmailNotVerifiedAndPasswordChangeRequired, UserStatus.EmailNotVerified)]
    [DataRow(VerificationType.PasswordReset, UserStatus.PasswordChangeRequired, UserStatus.OK)]
    public async Task VerificationDao_VerifyToken(
        VerificationType verificationType,
        UserStatus currentStatus,
        UserStatus newStatus)
    {
        await RunVerifyTokenTest(
            verificationType,
            currentStatus,
            newStatus,
            VerificationTokenVerificationResult.Verified,
            true)
            .ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(VerificationType.EmailVerification, UserStatus.PasswordChangeRequired)]
    [DataRow(VerificationType.EmailVerification, UserStatus.Suspended)]
    [DataRow(VerificationType.PasswordReset, UserStatus.EmailNotVerified)]
    [DataRow(VerificationType.PasswordReset, UserStatus.Suspended)]
    public async Task VerificationDao_VerifyToken_StatusNotUpdatable(
        VerificationType verificationType,
        UserStatus userStatus)
    {
        await RunVerifyTokenTest(
            verificationType,
            userStatus,
            userStatus,
            VerificationTokenVerificationResult.Verified,
            false)
            .ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task VerificationDao_VerifyToken_Verification_DoesNotExist()
    {
        var now = DateTime.UtcNow;
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCollection = GetMockVerificationCollection(now, cancellationToken: cancellationToken);

        var result = await RunVerifyTokenTest(
            now,
            TimeSpan.Zero,
            mockVerificationCollection: mockCollection,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(VerificationTokenVerificationResult.NotVerified, result);
    }

    private static void AssertGenerateVerificationTokenTest(
        DateTime? expirationDate,
        VerificationToken verificationToken)
    {
        Assert.IsNotNull(verificationToken);
        Assert.AreEqual(EmailAddress, verificationToken.EmailAddress);
        Assert.AreEqual(expirationDate, verificationToken.ExpirationDate);

        var pepper = verificationToken.Pepper;

        Assert.AreEqual(pepper.Length, TestUtilities.CalculateBase64Length(SaltAndPepperLength));
        Assert.IsTrue(Regex.IsMatch(pepper, Regexes.Base64String));

        var token = verificationToken.Token;
        using var hash = HashAlgorithm.Create(HashName.Name!);

        Assert.AreEqual(token.Length, TestUtilities.CalculateBase64Length(hash!.HashSize / 8));
        Assert.IsTrue(Regex.IsMatch(token, Regexes.Base64String));
    }

    private static Mock<IMongoCollection<UserDocument>> GetMockUserCollection(
        UserStatus userStatus = UserStatus.EmailNotVerified,
        CancellationToken cancellationToken = default)
    {
        var mockCursor = new Mock<IAsyncCursor<UserStatus>>();
        _ = mockCursor.Setup(m => m.MoveNextAsync(cancellationToken))
            .ReturnsAsync(true);
        _ = mockCursor.Setup(m => m.Current)
            .Returns(new List<UserStatus>() { userStatus });
        _ = mockCursor.Setup(m => m.Dispose());

        var mockCollection = new Mock<IMongoCollection<UserDocument>>();
        _ = mockCollection.Setup(
            m => m.FindAsync(
                It.IsAny<FilterDefinition<UserDocument>>(),
                It.IsAny<FindOptions<UserDocument, UserStatus>>(),
                cancellationToken))
            .Callback<FilterDefinition<UserDocument>, FindOptions<UserDocument, UserStatus>, CancellationToken>(
                (f, o, ct) =>
                {
                    var user = GetUser();
                    TestUtilities.AssertExpressionFilter(f, user);
                    TestUtilities.AssertProjection(o.Projection, user, user.UserStatus);
                })
            .ReturnsAsync(mockCursor.Object);

        return mockCollection;
    }

    private static Mock<IMongoCollection<Verification>> GetMockVerificationCollection(
        DateTime now,
        Verification? verification = default,
        bool assertFilter = true,
        CancellationToken cancellationToken = default)
    {
        var mockCursor = new Mock<IAsyncCursor<Verification>>();
        _ = mockCursor.Setup(m => m.MoveNextAsync(cancellationToken))
            .ReturnsAsync(true);
        _ = mockCursor.Setup(m => m.Current)
            .Returns(verification != null ? new List<Verification>() { verification } : new List<Verification>());
        _ = mockCursor.Setup(m => m.Dispose());

        var mockVerificationCollection = new Mock<IMongoCollection<Verification>>();
        verification ??= GetVerification(now);
        _ = mockVerificationCollection.Setup(m => m.InsertOneAsync(verification, default, cancellationToken))
            .Returns(Task.CompletedTask);

        var setup = mockVerificationCollection.Setup(
            m => m.FindAsync(
                It.IsAny<FilterDefinition<Verification>>(),
                default(FindOptions<Verification, Verification>),
                cancellationToken))
            .ReturnsAsync(mockCursor.Object);

        if (assertFilter)
        {
            _ = setup.Callback<FilterDefinition<Verification>, FindOptions<Verification, Verification>, CancellationToken>(
                (f, o, t) => TestUtilities.AssertExpressionFilter(f, verification));
        }

        return mockVerificationCollection;
    }

    private static UserDocument GetUser(UserStatus userStatus = UserStatus.EmailNotVerified)
    {
        return new UserDocument()
        {
            Id = UserId,
            UserStatus = userStatus,
        };
    }

    private static VerificationDao GetTarget()
    {
        return new VerificationDao(
            new Mock<IDateTimeProvider>().Object,
            new Mock<IOptions<VerificationOptions>>().Object,
            new Mock<IMongoCollection<UserDocument>>().Object,
            new Mock<IMongoCollection<Verification>>().Object);
    }

    private static Verification GetVerification(
        DateTime now,
        VerificationType verificationType = VerificationType.PasswordReset)
    {
        return new Verification()
        {
            CreationDate = now - MinVerificationAge - TimeSpan.FromSeconds(1),
            EmailAddress = EmailAddress,
            ExpirationDate = now + MaxVerificationAge,
            Salt = RandomNumberGenerator.GetBytes(SaltAndPepperLength),
            Type = verificationType,
            UserId = UserId,
        };
    }

    private static async Task<VerificationToken> RunGenerateVerificationTokenTest(
        DateTime now,
        TimeSpan? maxVerificationAge,
        Verification? verification = default,
        Mock<IMongoCollection<Verification>>? mockVerificationCollection = default,
        bool assertFilter = true,
        CancellationToken cancellationToken = default)
    {
        var mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _ = mockDateTimeProvider.Setup(m => m.UtcNow)
            .Returns(now);
        var mockOptions = new Mock<IOptions<VerificationOptions>>();
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new VerificationOptions()
            {
                HashName = HashName,
                MaxVerificationAge = maxVerificationAge,
                MinVerificationAge = MinVerificationAge,
                PepperLength = SaltAndPepperLength,
                SaltLength = SaltAndPepperLength,
            });
        mockVerificationCollection ??= GetMockVerificationCollection(
            now,
            verification,
            assertFilter,
            cancellationToken);

        var target = new VerificationDao(
            mockDateTimeProvider.Object,
            mockOptions.Object,
            new Mock<IMongoCollection<UserDocument>>().Object,
            mockVerificationCollection.Object);

        return await target.GenerateVerificationToken(
            UserId,
            EmailAddress,
            VerificationType.PasswordReset,
            cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task RunVerifyTokenTest(
        VerificationType verificationType,
        UserStatus currentUserStatus,
        UserStatus newUserStatus,
        VerificationTokenVerificationResult expectedResult,
        bool assertUpdateUserStatus)
    {
        var now = DateTime.UtcNow;
        var verification = GetVerification(now, verificationType);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserCollection = GetMockUserCollection(currentUserStatus, cancellationToken);
        Expression<Func<IMongoCollection<UserDocument>, Task<UpdateResult>>> updateUserStatus =
            m => m.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserDocument>>(),
                It.IsAny<UpdateDefinition<UserDocument>>(),
                default,
                cancellationToken);
        _ = mockUserCollection.Setup(updateUserStatus)
            .Callback<FilterDefinition<UserDocument>, UpdateDefinition<UserDocument>, UpdateOptions, CancellationToken>(
                (f, u, o, ct) =>
                {
                    TestUtilities.AssertUpdate(
                        f,
                        u,
                        GetUser(currentUserStatus),
                        nameof(UserDocument.UserStatus),
                        newUserStatus);
                })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, default));
        var mockVerificationCollection = GetMockVerificationCollection(
            now,
            verification,
            cancellationToken: cancellationToken);
        Expression<Func<IMongoCollection<Verification>, Task<UpdateResult>>> updateVerification =
            m => m.UpdateOneAsync(
                It.IsAny<FilterDefinition<Verification>>(),
                It.IsAny<UpdateDefinition<Verification>>(),
                default,
                cancellationToken);
        _ = mockVerificationCollection.Setup(updateVerification)
            .Callback<FilterDefinition<Verification>, UpdateDefinition<Verification>, UpdateOptions, CancellationToken>(
                (f, u, o, c) =>
                {
                    TestUtilities.AssertUpdate<Verification, DateTime?>(
                        f,
                        u,
                        verification,
                        nameof(Verification.ExpirationDate),
                        now);
                })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, default));

        var actualResult = await RunVerifyTokenTest(
            now,
            TimeSpan.Zero,
            verification: verification,
            mockUserCollection: mockUserCollection,
            mockVerificationCollection: mockVerificationCollection,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expectedResult, actualResult);
        mockVerificationCollection.Verify(updateVerification);

        if (assertUpdateUserStatus)
        {
            mockUserCollection.Verify(updateUserStatus);
        }
    }

    private static async Task<VerificationTokenVerificationResult> RunVerifyTokenTest(
        DateTime now,
        TimeSpan creationDateOffset,
        string emailAddressOffset = "",
        string pepperOffset = "",
        string tokenOffset = "",
        Verification? verification = default,
        Mock<IMongoCollection<UserDocument>>? mockUserCollection = default,
        Mock<IMongoCollection<Verification>>? mockVerificationCollection = default,
        bool assertFilter = true,
        CancellationToken cancellationToken = default)
    {
        var mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _ = mockDateTimeProvider.Setup(m => m.UtcNow)
            .Returns(now);
        mockUserCollection ??= GetMockUserCollection(cancellationToken: cancellationToken);
        mockVerificationCollection ??= GetMockVerificationCollection(
            now,
            verification,
            assertFilter,
            cancellationToken);
        var mockOptions = new Mock<IOptions<VerificationOptions>>();
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new VerificationOptions()
            {
                HashName = HashName,
                MaxVerificationAge = MaxVerificationAge,
                MinVerificationAge = MinVerificationAge,
                PepperLength = SaltAndPepperLength,
                SaltLength = SaltAndPepperLength,
            });
        var target = new VerificationDao(
            mockDateTimeProvider.Object,
            mockOptions.Object,
            mockUserCollection.Object,
            mockVerificationCollection.Object);
        verification ??= GetVerification(now);

        var offsetVerification = new Verification()
        {
            CreationDate = verification.CreationDate - creationDateOffset,
            EmailAddress = EmailAddress + emailAddressOffset,
            ExpirationDate = verification.ExpirationDate,
            Salt = verification.Salt,
        };

        var verificationToken = await VerificationToken.Create(
            offsetVerification,
            RandomNumberGenerator.GetBytes(SaltAndPepperLength),
            HashName,
            cancellationToken)
            .ConfigureAwait(false);

        var token = tokenOffset + verificationToken.Token;
        var pepper = pepperOffset + verificationToken.Pepper;

        return await target.VerifyToken(
            UserId,
            EmailAddress,
            verification.Type,
            token,
            pepper,
            cancellationToken)
            .ConfigureAwait(false);
    }
}
