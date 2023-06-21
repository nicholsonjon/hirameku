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
using Hirameku.TestTools;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System;
using System.Linq.Expressions;
using System.Threading;

[TestClass]
public class PasswordDaoTests
{
    private const string BannedPassword = nameof(BannedPassword);
    private const string UserId = nameof(UserId);
    private static readonly TimeSpan MaxPasswordAge = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MinPasswordAge = TimeSpan.FromMinutes(1);
    private static readonly DateTime Now = DateTime.UtcNow;

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordDao_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(VerifyPasswordResult.NotVerified, true)]
    [DataRow(VerifyPasswordResult.VerifiedAndRehashRequired, true)]
    [DataRow(VerifyPasswordResult.Verified, false)]
    public async Task PasswordDao_SavePassword_OverwriteExisting(
        VerifyPasswordResult verifyPasswordResult,
        bool disallowSavingIdenticalPasswords)
    {
        var user = GetUser();
        var passwordHash = user.PasswordHash!;
        var passwordOptions = new PasswordOptions()
        {
            DisallowSavingIdenticalPasswords = disallowSavingIdenticalPasswords,
            MaxPasswordAge = MaxPasswordAge,
            MinPasswordAge = MinPasswordAge,
            Version = passwordHash.Version,
        };
        var hashPasswordResult = new HashPasswordResult(passwordHash.Hash, passwordHash.Salt, passwordHash.Version);
        var mockPasswordHasher = GetMockPasswordHasher(passwordHash, hashPasswordResult, verifyPasswordResult);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCollection = GetMockCollection(user, cancellationToken);
        var updateOneAsync = SetupUpdate(
            mockCollection,
            user,
            hashPasswordResult,
            Now + MaxPasswordAge,
            cancellationToken);

        await RunSavePasswordTest(
            user,
            passwordOptions,
            mockPasswordHasher,
            mockCollection,
            cancellationToken)
            .ConfigureAwait(false);

        mockCollection.Verify(updateOneAsync);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(UserStatus.EmailNotVerifiedAndPasswordChangeRequired, UserStatus.EmailNotVerified)]
    [DataRow(UserStatus.PasswordChangeRequired, UserStatus.OK)]
    public async Task PasswordDao_SavePassword_PasswordChangeRequired(
        UserStatus currentUserStatus,
        UserStatus updatedUserStatus)
    {
        var user = GetUser();
        user.UserStatus = currentUserStatus;
        var passwordHash = user.PasswordHash!;
        var hashPasswordResult = new HashPasswordResult(passwordHash.Hash, passwordHash.Salt, passwordHash.Version);
        var mockPasswordHasher = GetMockPasswordHasher(hashPasswordResult: hashPasswordResult);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCollection = GetMockCollection(user, cancellationToken);
        var updateOneAsync = SetupUpdate(
            mockCollection,
            user,
            hashPasswordResult,
            Now + MaxPasswordAge,
            cancellationToken,
            updatedUserStatus);

        await RunSavePasswordTest(
            user,
            mockPasswordHasher: mockPasswordHasher,
            mockCollection: mockCollection,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        mockCollection.Verify(updateOneAsync);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(PasswordException))]
    public async Task PasswordDao_SavePassword_PasswordChangeTooRecent_Throws()
    {
        var user = GetUser();
        user.PasswordHash!.LastChangeDate = Now;

        await RunSavePasswordTest(user).ConfigureAwait(false);

        Assert.Fail(nameof(PasswordException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(PasswordDao_SavePassword_Password_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(PasswordDao_SavePassword_Password_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(PasswordDao_SavePassword_Password_Throws) + "(WhiteSpace)")]
    public async Task PasswordDao_SavePassword_Password_Throws(string password)
    {
        var target = GetTarget();

        await target.SavePassword(UserId, password).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(PasswordException))]
    public async Task PasswordDao_SavePassword_PasswordIsIdentical_Throws()
    {
        var user = GetUser();
        var passwordHash = user.PasswordHash!;
        var passwordOptions = new PasswordOptions()
        {
            DisallowSavingIdenticalPasswords = true,
            MaxPasswordAge = MaxPasswordAge,
            MinPasswordAge = MinPasswordAge,
            Version = passwordHash!.Version,
        };
        var mockPasswordHasher = GetMockPasswordHasher(passwordHash);

        await RunSavePasswordTest(user, passwordOptions, mockPasswordHasher).ConfigureAwait(false);

        Assert.Fail(nameof(PasswordException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(true)]
    [DataRow(false)]
    public async Task PasswordDao_SavePassword(bool doPasswordsExpire)
    {
        var user = GetUser();
        user.PasswordHash = null;
        var hashPasswordResult = new HashPasswordResult(
            TestData.GetHMACSHA512HashedPasswordBytes(),
            TestData.GetHMACSHA512SaltBytes(),
            PasswordHashVersion.HMACSHA512);
        var mockPasswordHasher = GetMockPasswordHasher(hashPasswordResult: hashPasswordResult);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCollection = GetMockCollection(user, cancellationToken);
        var maxPasswordAge = doPasswordsExpire ? MaxPasswordAge : null as TimeSpan?;
        var expirationDate = maxPasswordAge.HasValue
            ? Now + maxPasswordAge.Value
            : null as DateTime?;
        var updateOneAsync = SetupUpdate(
            mockCollection,
            user,
            hashPasswordResult,
            expirationDate,
            cancellationToken);

        await RunSavePasswordTest(
            user,
            new PasswordOptions()
            {
                DisallowSavingIdenticalPasswords = false,
                MaxPasswordAge = maxPasswordAge,
                MinPasswordAge = MinPasswordAge,
                Version = PasswordHashVersion.Current,
            },
            mockPasswordHasher,
            mockCollection,
            cancellationToken)
            .ConfigureAwait(false);

        mockCollection.Verify(updateOneAsync);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task PasswordDao_SavePassword_UserDoesNotExist_Throws()
    {
        await RunSavePasswordTest().ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(PasswordDao_SavePassword_UserId_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(PasswordDao_SavePassword_UserId_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(PasswordDao_SavePassword_UserId_Throws) + "(WhiteSpace)")]
    public async Task PasswordDao_SavePassword_UserId_Throws(string userId)
    {
        var target = GetTarget();

        await target.SavePassword(userId, TestData.Password).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PasswordDao_VerifyPassword_NotVerified()
    {
        var user = GetUser();
        var mockPasswordHasher = GetMockPasswordHasher(
            user.PasswordHash!,
            verifyPasswordResult: VerifyPasswordResult.NotVerified);

        var result = await RunVerifyPasswordTest(user, mockPasswordHasher: mockPasswordHasher)
            .ConfigureAwait(false);

        Assert.AreEqual(PasswordVerificationResult.NotVerified, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(PasswordDao_VerifyPassword_Password_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(PasswordDao_VerifyPassword_Password_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(PasswordDao_VerifyPassword_Password_Throws) + "(WhiteSpace)")]
    public async Task PasswordDao_VerifyPassword_Password_Throws(string password)
    {
        var target = GetTarget();

        _ = await target.VerifyPassword(UserId, password).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task PasswordDao_VerifyPassword_UserDoesNotExist_Throws()
    {
        _ = await RunVerifyPasswordTest().ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PasswordDao_VerifyPassword()
    {
        var user = GetUser();
        var mockPasswordHasher = GetMockPasswordHasher(user.PasswordHash!);

        var result = await RunVerifyPasswordTest(user, mockPasswordHasher: mockPasswordHasher)
            .ConfigureAwait(false);

        Assert.AreEqual(PasswordVerificationResult.Verified, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PasswordDao_VerifyPassword_VerifiedAndExpired()
    {
        var user = GetUser();
        var passwordHash = user.PasswordHash!;
        passwordHash.ExpirationDate = DateTime.MinValue;
        var mockPasswordHasher = GetMockPasswordHasher(passwordHash);

        var result = await RunVerifyPasswordTest(user, mockPasswordHasher: mockPasswordHasher)
            .ConfigureAwait(false);

        Assert.AreEqual(PasswordVerificationResult.VerifiedAndExpired, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PasswordDao_VerifyPassword_VerifiedAndRehashed()
    {
        var user = GetUser();
        var passwordHash = user.PasswordHash!;
        var hashPasswordResult = new HashPasswordResult(passwordHash.Hash, passwordHash.Salt, passwordHash.Version);
        var mockPasswordHasher = GetMockPasswordHasher(
            passwordHash,
            hashPasswordResult,
            VerifyPasswordResult.VerifiedAndRehashRequired);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCollection = GetMockCollection(user, cancellationToken);
        var updateOneAsync = SetupUpdate(
            mockCollection,
            user,
            hashPasswordResult,
            Now + MaxPasswordAge,
            cancellationToken);

        var result = await RunVerifyPasswordTest(
            user,
            mockPasswordHasher,
            mockCollection,
            cancellationToken)
            .ConfigureAwait(false);

        mockCollection.Verify(updateOneAsync);
        Assert.AreEqual(PasswordVerificationResult.Verified, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(PasswordDao_VerifyPassword_UserName_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(PasswordDao_VerifyPassword_UserName_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(PasswordDao_VerifyPassword_UserName_Throws) + "(WhiteSpace)")]
    public async Task PasswordDao_VerifyPassword_UserName_Throws(string userName)
    {
        var target = GetTarget();

        _ = await target.VerifyPassword(userName, TestData.Password).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    private static void AssertPasswordHash(
        PasswordHash? passwordHash,
        HashPasswordResult hashPasswordResult,
        DateTime? expectedExpirationDate)
    {
        Assert.AreEqual(expectedExpirationDate, passwordHash?.ExpirationDate);
        Assert.AreEqual(hashPasswordResult.Hash, passwordHash?.Hash);
        Assert.AreEqual(Now, passwordHash?.LastChangeDate);
        Assert.AreEqual(hashPasswordResult.Salt, passwordHash?.Salt);
        Assert.AreEqual(hashPasswordResult.Version, passwordHash?.Version);
    }

    private static Mock<IMongoCollection<UserDocument>> GetMockCollection(
        UserDocument? user = default,
        CancellationToken cancellationToken = default)
    {
        var mockCursor = new Mock<IAsyncCursor<UserDocument>>();
        _ = mockCursor.Setup(m => m.MoveNextAsync(cancellationToken))
            .ReturnsAsync(true);
        _ = mockCursor.Setup(m => m.Current)
            .Returns(user != null ? new List<UserDocument>() { user } : new List<UserDocument>());
        _ = mockCursor.Setup(m => m.Dispose());

        var mockCollection = new Mock<IMongoCollection<UserDocument>>();
        _ = mockCollection.Setup(
            m => m.FindAsync(
                It.IsAny<FilterDefinition<UserDocument>>(),
                default(FindOptions<UserDocument, UserDocument>),
                cancellationToken))
            .Callback<FilterDefinition<UserDocument>, FindOptions<UserDocument, UserDocument>, CancellationToken>(
                (f, o, t) => TestUtilities.AssertExpressionFilter(f, user ?? new UserDocument() { Id = UserId }))
            .ReturnsAsync(mockCursor.Object);

        return mockCollection;
    }

    private static Mock<IDateTimeProvider> GetMockDateTimeProvider()
    {
        var mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _ = mockDateTimeProvider.Setup(m => m.UtcNow)
            .Returns(Now);

        return mockDateTimeProvider;
    }

    private static Mock<IPasswordHasher> GetMockPasswordHasher(
        PasswordHash? passwordHash = default,
        HashPasswordResult? hashPasswordResult = default,
        VerifyPasswordResult verifyPasswordResult = VerifyPasswordResult.Verified)
    {
        var mockPasswordHasher = new Mock<IPasswordHasher>();
        passwordHash ??= GetUser().PasswordHash!;
        var hash = passwordHash.Hash;
        var salt = passwordHash.Salt;
        var version = passwordHash.Version;
        hashPasswordResult ??= new HashPasswordResult(hash, salt, version);
        _ = mockPasswordHasher.Setup(m => m.HashPassword(TestData.Password))
            .Returns(hashPasswordResult);
        _ = mockPasswordHasher.Setup(m => m.VerifyPassword(
            passwordHash.Version,
            passwordHash.Salt,
            passwordHash.Hash,
            TestData.Password))
            .Returns(verifyPasswordResult);

        return mockPasswordHasher;
    }

    private static Mock<IOptions<PasswordOptions>> GetMockPasswordOptions(PasswordOptions? options = default)
    {
        options ??= new PasswordOptions()
        {
            DisallowSavingIdenticalPasswords = false,
            MaxPasswordAge = MaxPasswordAge,
            MinPasswordAge = MinPasswordAge,
            Version = PasswordHashVersion.Current,
        };
        var mockPasswordOptions = new Mock<IOptions<PasswordOptions>>();
        _ = mockPasswordOptions.Setup(m => m.Value)
            .Returns(options);

        return mockPasswordOptions;
    }

    private static PasswordDao GetTarget()
    {
        return new PasswordDao(
            new Mock<IMongoCollection<UserDocument>>().Object,
            new Mock<IDateTimeProvider>().Object,
            new Mock<IOptions<PasswordOptions>>().Object,
            new Mock<IPasswordHasher>().Object);
    }

    private static UserDocument GetUser()
    {
        return new UserDocument()
        {
            Id = UserId,
            PasswordHash = new PasswordHash()
            {
                Hash = TestData.GetHMACSHA512HashedPasswordBytes(),
                Salt = TestData.GetHMACSHA512SaltBytes(),
                Version = PasswordHashVersion.HMACSHA512,
            },
        };
    }

    private static async Task RunSavePasswordTest(
        UserDocument? user = default,
        PasswordOptions? passwordOptions = default,
        Mock<IPasswordHasher>? mockPasswordHasher = default,
        Mock<IMongoCollection<UserDocument>>? mockCollection = default,
        CancellationToken cancellationToken = default)
    {
        mockCollection ??= GetMockCollection(user, cancellationToken);

        var target = new PasswordDao(
            mockCollection.Object,
            GetMockDateTimeProvider().Object,
            GetMockPasswordOptions(passwordOptions).Object,
            (mockPasswordHasher ?? new Mock<IPasswordHasher>()).Object);

        await target.SavePassword(UserId, TestData.Password, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<PasswordVerificationResult> RunVerifyPasswordTest(
        UserDocument? user = default,
        Mock<IPasswordHasher>? mockPasswordHasher = default,
        Mock<IMongoCollection<UserDocument>>? mockCollection = default,
        CancellationToken cancellationToken = default)
    {
        mockCollection ??= GetMockCollection(user, cancellationToken);

        var target = new PasswordDao(
            mockCollection.Object,
            GetMockDateTimeProvider().Object,
            GetMockPasswordOptions().Object,
            (mockPasswordHasher ?? new Mock<IPasswordHasher>()).Object);

        return await target.VerifyPassword(UserId, TestData.Password, cancellationToken).ConfigureAwait(false);
    }

    private static Expression<Func<IMongoCollection<UserDocument>, Task<UpdateResult>>> SetupUpdate(
        Mock<IMongoCollection<UserDocument>> mockCollection,
        UserDocument user,
        HashPasswordResult hashPasswordResult,
        DateTime? expirationDate,
        CancellationToken cancellationToken,
        UserStatus? updatedUserStatus = default)
    {
        Expression<Func<IMongoCollection<UserDocument>, Task<UpdateResult>>> updateOneAsync =
            m => m.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserDocument>>(),
                It.IsAny<UpdateDefinition<UserDocument>>(),
                default,
                cancellationToken);

        _ = mockCollection.Setup(updateOneAsync)
            .Callback<FilterDefinition<UserDocument>, UpdateDefinition<UserDocument>, UpdateOptions, CancellationToken>(
                (f, u, o, c) =>
                {
                    const string PasswordHash = nameof(UserDocument.PasswordHash);
                    const string UserStatus = nameof(UserDocument.UserStatus);

                    if (TestUtilities.IsUpdateFor<UserDocument, PasswordHash>(u, PasswordHash))
                    {
                        TestUtilities.AssertUpdate(
                            f,
                            u,
                            user,
                            PasswordHash,
                            (PasswordHash? ph) => AssertPasswordHash(ph, hashPasswordResult, expirationDate));
                    }
                    else if (TestUtilities.IsUpdateFor<UserDocument, UserStatus>(u, UserStatus))
                    {
                        TestUtilities.AssertUpdate(
                            f,
                            u,
                            user,
                            UserStatus,
                            (UserStatus? us) => Assert.AreEqual(updatedUserStatus, us));
                    }
                    else
                    {
                        Assert.Inconclusive("Unexpected update operation encountered");
                    }
                })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, default));

        return updateOneAsync;
    }
}
