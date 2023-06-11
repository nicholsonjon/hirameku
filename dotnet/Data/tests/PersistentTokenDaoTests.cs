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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using JsonConvert = Newtonsoft.Json.JsonConvert;

[TestClass]
public class PersistentTokenDaoTests
{
    private const string ClientId = nameof(ClientId);
    private const string ClientToken = nameof(ClientToken);
    private const int ClientTokenLength = 32;
    private const string UserId = nameof(UserId);
    private const string UserName = nameof(UserName);
    private static readonly byte[] HashedPassword = TestData.GetHMACSHA512HashedPasswordBytes();
    private static readonly TimeSpan MaxTokenAge = new(365, 0, 0, 0);
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly byte[] PersistentTokenHash = Convert.FromBase64String("7my3Gcr73N3BkeRPOAh8t6+CzIBls0MJlkrJvn7KPu6otkrkXnxzAB1y9r37HddFRuhCjPR/GifXVtUMk8b3Yw==");

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PersistentTokenDao_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PersistentTokenDao_SavePersistentToken_InsertPersistentToken()
    {
        var user = GetUser();
        user.PersistentTokens = null;

        await RunSavePersistentTokenTest(user).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(PersistentTokenDao_SavePersistentToken_ClientId_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(PersistentTokenDao_SavePersistentToken_ClientId_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(PersistentTokenDao_SavePersistentToken_ClientId_Throws) + "(WhiteSpace)")]
    public async Task PersistentTokenDao_SavePersistentToken_ClientId_Throws(string clientId)
    {
        var target = GetTarget();

        _ = await target.SavePersistentToken(UserId, clientId, ClientToken).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(PersistentTokenDao_SavePersistentToken_ClientToken_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(PersistentTokenDao_SavePersistentToken_ClientToken_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(PersistentTokenDao_SavePersistentToken_ClientToken_Throws) + "(WhiteSpace)")]
    public async Task PersistentTokenDao_SavePersistentToken_ClientToken_Throws(string clientToken)
    {
        var target = GetTarget();

        _ = await target.SavePersistentToken(UserId, ClientId, clientToken).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task PersistentTokenDao_SavePersistentToken_NoStoredPasswordHash_Throws()
    {
        await RunSavePersistentTokenTest(new UserDocument() { Id = UserId }).ConfigureAwait(false);

        Assert.Fail(nameof(InvalidOperationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PersistentTokenDao_SavePersistentToken_PurgeExpiredTokens()
    {
        var user = GetUser();
        var persistentTokens = new List<PersistentToken>(user.PersistentTokens!)
        {
            new PersistentToken() { ClientId = ClientId, },
        };
        user.PersistentTokens = persistentTokens;
        var mockCollection = GetMockCollection(user);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _ = mockCollection.Setup(m => m.DeleteOneAsync(It.IsAny<FilterDefinition<UserDocument>>(), cancellationToken))
            .Callback<FilterDefinition<UserDocument>, CancellationToken>(
                (f, t) =>
                {
                    Assert.IsInstanceOfType(f, typeof(ExpressionFilterDefinition<UserDocument>));
                    var expression = ((ExpressionFilterDefinition<UserDocument>)f).Expression.Compile();
                    Assert.IsTrue(expression(user));
                })
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        await RunSavePersistentTokenTest(user, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PersistentTokenDao_SavePersistentToken_UpdatePersistentToken()
    {
        var user = GetUser();
        var persistentToken = user.PersistentTokens!.Single();
        persistentToken.ExpirationDate = DateTime.UtcNow + TimeSpan.FromMinutes(1);
        persistentToken.Hash = new byte[] { 0 };

        await RunSavePersistentTokenTest(user).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task PersistentTokenDao_SavePersistentToken_UserDoesNotExist_Throws()
    {
        await RunSavePersistentTokenTest().ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(PersistentTokenDao_SavePersistentToken_UserName_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(PersistentTokenDao_SavePersistentToken_UserName_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(PersistentTokenDao_SavePersistentToken_UserName_Throws) + "(WhiteSpace)")]
    public async Task PersistentTokenDao_SavePersistentToken_UserName_Throws(string userId)
    {
        var target = GetTarget();

        _ = await target.SavePersistentToken(userId, ClientId, ClientToken).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(PersistentTokenDao_VerifyPersistentToken_ClientId_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(PersistentTokenDao_VerifyPersistentToken_ClientId_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(PersistentTokenDao_VerifyPersistentToken_ClientId_Throws) + "(WhiteSpace)")]
    public async Task PersistentTokenDao_VerifyPersistentToken_ClientId_Throws(string clientId)
    {
        var target = GetTarget();

        _ = await target.VerifyPersistentToken(UserName, clientId, ClientToken).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(PersistentTokenDao_VerifyPersistentToken_ClientToken_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(PersistentTokenDao_VerifyPersistentToken_ClientToken_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(PersistentTokenDao_VerifyPersistentToken_ClientToken_Throws) + "(WhiteSpace)")]
    public async Task PersistentTokenDao_VerifyPersistentToken_ClientToken_Throws(string clientToken)
    {
        var target = GetTarget();

        _ = await target.VerifyPersistentToken(UserName, ClientId, clientToken).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PersistentTokenDao_VerifyPersistentToken_NoTokenAvailable()
    {
        var user = GetUser();
        user.PersistentTokens = null;

        var result = await RunVerifyPersistentTokenTest(user).ConfigureAwait(false);

        Assert.AreEqual(PersistentTokenVerificationResult.NoTokenAvailable, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PersistentTokenDao_VerifyPersistentToken_NotVerified()
    {
        var user = GetUser();

        var result = await RunVerifyPersistentTokenTest(user, passwordResult: VerifyPasswordResult.NotVerified)
            .ConfigureAwait(false);

        Assert.AreEqual(PersistentTokenVerificationResult.NotVerified, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PersistentTokenDao_VerifyPersistentToken_PurgeExpiredTokens()
    {
        var user = GetUser();
        user.PersistentTokens = new List<PersistentToken>(user.PersistentTokens!) { new PersistentToken() };
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCollection = GetMockCollection(user, cancellationToken);
        _ = mockCollection.Setup(m => m.DeleteManyAsync(It.IsAny<FilterDefinition<UserDocument>>(), cancellationToken))
            .Callback<FilterDefinition<UserDocument>, CancellationToken>(
                (f, t) => TestUtilities.AssertExpressionFilter(f, user))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        var result = await RunVerifyPersistentTokenTest(
            GetUser(),
            passwordResult: VerifyPasswordResult.Verified,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        Assert.AreEqual(PersistentTokenVerificationResult.Verified, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task PersistentTokenDao_VerifyPersistentToken_UserDoesNotExist_Throws()
    {
        _ = await RunVerifyPersistentTokenTest().ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(PersistentTokenDao_VerifyPersistentToken_UserName_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(PersistentTokenDao_VerifyPersistentToken_UserName_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(PersistentTokenDao_VerifyPersistentToken_UserName_Throws) + "(WhiteSpace)")]
    public async Task PersistentTokenDao_VerifyPersistentToken_UserName_Throws(string userName)
    {
        var target = GetTarget();

        _ = await target.VerifyPersistentToken(userName, ClientId, ClientToken).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PersistentTokenDao_VerifyPersistentToken_Verified()
    {
        var result = await RunVerifyPersistentTokenTest(GetUser(), passwordResult: VerifyPasswordResult.Verified)
            .ConfigureAwait(false);

        Assert.AreEqual(PersistentTokenVerificationResult.Verified, result);
    }

    private static void AssertFilter(FilterDefinition<UserDocument> filter)
    {
        var document = filter.Render(
            BsonSerializer.LookupSerializer<UserDocument>(),
            BsonSerializer.SerializerRegistry);
        var json = document.ToJson();
        var updateFilter = JsonConvert.DeserializeObject<PersistentTokenUpdateFilter>(json);

        Assert.AreEqual(UserId, updateFilter?.UserId);
        Assert.AreEqual(ClientId, updateFilter?.PersistentTokens?.ElemMatch?.ClientId);
    }

    private static void AssertUpdate(UpdateDefinition<UserDocument> update)
    {
        var type = update.GetType();
        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        var opName = type.GetField("_operatorName", bindingFlags)?.GetValue(update) as string;
        var field = type.GetField("_field", bindingFlags)?.GetValue(update)
            as ExpressionFieldDefinition<UserDocument, PersistentToken>;
        var renderedField = field?.Render(
            BsonSerializer.LookupSerializer<UserDocument>(),
            BsonSerializer.SerializerRegistry);
        var actualToken = (PersistentToken?)type.GetField("_value", bindingFlags)?.GetValue(update);
        var expectedToken = GetPersistentToken();

        Assert.AreEqual("$set", opName);
        Assert.AreEqual(nameof(UserDocument.PersistentTokens) + ".$", renderedField?.FieldName);
        Assert.AreEqual(expectedToken.ClientId, actualToken?.ClientId);
        Assert.AreEqual(expectedToken.ExpirationDate, actualToken?.ExpirationDate);
        Assert.IsTrue(Enumerable.SequenceEqual(expectedToken.Hash, actualToken?.Hash!));
    }

    private static void AssertUpdate(
        FilterDefinition<UserDocument> filter,
        UpdateDefinition<UserDocument> update,
        UpdateOptions options)
    {
        AssertFilter(filter);
        AssertUpdate(update);
        Assert.IsTrue(options.IsUpsert);
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
                (f, o, t) => TestUtilities.AssertExpressionFilter(f, user ?? GetUser()))
            .ReturnsAsync(mockCursor.Object);

        return mockCollection;
    }

    private static PersistentTokenDao GetTarget()
    {
        return new PersistentTokenDao(
            new Mock<IMongoCollection<UserDocument>>().Object,
            new Mock<IDateTimeProvider>().Object,
            new Mock<IOptions<PersistentTokenOptions>>().Object,
            new Mock<IPasswordHasher>().Object);
    }

    private static PersistentToken GetPersistentToken()
    {
        return new PersistentToken()
        {
            ClientId = ClientId,
            ExpirationDate = Now + MaxTokenAge,
            Hash = PersistentTokenHash,
        };
    }

    private static UserDocument GetUser()
    {
        return new UserDocument()
        {
            Id = UserId,
            PasswordHash = new PasswordHash()
            {
                Hash = HashedPassword,
            },
            PersistentTokens = new List<PersistentToken>()
            {
                GetPersistentToken(),
            },
            UserName = UserName,
        };
    }

    private static async Task RunSavePersistentTokenTest(
        UserDocument? user = default,
        Mock<IMongoCollection<UserDocument>>? mockCollection = default,
        CancellationToken cancellationToken = default)
    {
        var mockDateTime = new Mock<IDateTimeProvider>();
        _ = mockDateTime.Setup(m => m.UtcNow)
            .Returns(Now);
        var mockPasswordHasher = new Mock<IPasswordHasher>();
        var persistentToken = GetPersistentToken();
        _ = mockPasswordHasher.Setup(m => m.HashPassword(ClientId + ClientToken, HashedPassword))
            .Returns(new HashPasswordResult(
                persistentToken.Hash,
                HashedPassword,
                PasswordHashVersion.HMACSHA512));
        var mockOptions = new Mock<IOptions<PersistentTokenOptions>>();
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new PersistentTokenOptions()
            {
                ClientTokenLength = ClientTokenLength,
                MaxTokenAge = MaxTokenAge,
            });

        mockCollection ??= GetMockCollection(user, cancellationToken);
        _ = mockCollection.Setup(
            m => m.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserDocument>>(),
                It.IsAny<UpdateDefinition<UserDocument>>(),
                It.IsAny<UpdateOptions>(),
                cancellationToken))
            .Callback<FilterDefinition<UserDocument>, UpdateDefinition<UserDocument>, UpdateOptions, CancellationToken>(
                (f, u, o, t) => AssertUpdate(f, u, o))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, default));

        var target = new PersistentTokenDao(
            mockCollection.Object,
            mockDateTime.Object,
            mockOptions.Object,
            mockPasswordHasher.Object);

        var expirationDate = await target.SavePersistentToken(UserId, ClientId, ClientToken, cancellationToken)
            .ConfigureAwait(false);

        Assert.AreEqual(persistentToken.ExpirationDate, expirationDate);
    }

    private static async Task<PersistentTokenVerificationResult> RunVerifyPersistentTokenTest(
        UserDocument? user = default,
        VerifyPasswordResult passwordResult = default,
        Mock<IMongoCollection<UserDocument>>? mockCollection = default,
        CancellationToken cancellationToken = default)
    {
        var mockDateTime = new Mock<IDateTimeProvider>();
        _ = mockDateTime.Setup(m => m.UtcNow)
            .Returns(Now);
        var mockPasswordHasher = new Mock<IPasswordHasher>();
        _ = mockPasswordHasher.Setup(
            m => m.VerifyPassword(
                It.IsAny<PasswordHashVersion>(),
                HashedPassword,
                PersistentTokenHash,
                ClientId + ClientToken))
            .Callback<PasswordHashVersion, byte[], byte[], string>(
                (v, s, p, t) => Assert.AreEqual(PasswordHashVersion.Current.Name, v.Name))
            .Returns(passwordResult);

        mockCollection ??= GetMockCollection(user, cancellationToken);

        var target = new PersistentTokenDao(
            mockCollection.Object,
            mockDateTime.Object,
            new Mock<IOptions<PersistentTokenOptions>>().Object,
            mockPasswordHasher.Object);

        return await target.VerifyPersistentToken(UserId, ClientId, ClientToken, cancellationToken)
            .ConfigureAwait(false);
    }

    [SuppressMessage("Performance", "CA1812", Justification = "Used for JSON serialization")]
    private sealed class ElemMatch
    {
        [JsonProperty("client_id")]
        public string? ClientId { get; set; }
    }

    [SuppressMessage("Performance", "CA1812", Justification = "Used for JSON serialization")]
    private sealed class PersistentTokensCriteria
    {
        [JsonProperty("$elemMatch")]
        public ElemMatch? ElemMatch { get; set; }
    }

    [SuppressMessage("Performance", "CA1812", Justification = "Used for JSON serialization")]
    private sealed class PersistentTokenUpdateFilter
    {
        public PersistentTokensCriteria? PersistentTokens { get; set; }

        [JsonProperty("_id")]
        public string? UserId { get; set; }
    }
}
