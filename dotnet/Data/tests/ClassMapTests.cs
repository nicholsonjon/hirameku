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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

[TestClass]
public class ClassMapTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_ClassMapsAreRegistered()
    {
        Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(AuthenticationEvent)));
        Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(Card)));
        Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(CardDocument)));
        Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(Deck)));
        Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(DeckDocument)));
        Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(Meaning)));
        Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(Review)));
        Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(ReviewDocument)));
        Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(PasswordHash)));
        Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(PersistentToken)));
        Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(User)));
        Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(UserDocument)));
        Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(Verification)));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_AuthenticationEvent_Serialization()
    {
        const string Accept = nameof(Accept);
        const AuthenticationResult AuthResult = AuthenticationResult.Authenticated;
        const string ContentEncoding = nameof(ContentEncoding);
        const string ContentLanguage = nameof(ContentLanguage);
        const string Hash = nameof(Hash);
        const string Id = nameof(Id);
        const string RemoteIP = nameof(RemoteIP);
        const string UserAgent = nameof(UserAgent);
        const string UserId = nameof(UserId);
        var expected = new AuthenticationEvent()
        {
            Accept = Accept,
            AuthenticationResult = AuthResult,
            ContentEncoding = ContentEncoding,
            ContentLanguage = ContentLanguage,
            Hash = Hash,
            Id = Id,
            RemoteIP = RemoteIP,
            UserAgent = UserAgent,
            UserId = UserId,
        };

        var actual = DoRoundtripSerialization(expected);

        Assert.AreEqual(Accept, actual.Accept);
        Assert.AreEqual(AuthResult, actual.AuthenticationResult);
        Assert.AreEqual(ContentEncoding, actual.ContentEncoding);
        Assert.AreEqual(ContentLanguage, actual.ContentLanguage);
        Assert.AreEqual(Hash, actual.Hash);
        Assert.AreEqual(Id, actual.Id);
        Assert.AreEqual(RemoteIP, actual.RemoteIP);
        Assert.AreEqual(UserAgent, actual.UserAgent);
        Assert.AreEqual(UserId, actual.UserId);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_Card_Serialization()
    {
        var expected = GetCard<Card>();

        var actual = DoRoundtripSerialization(expected);

        AssertCard(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_CardDocument_Serialization()
    {
        var expected = GetCard<CardDocument>();
        expected.Id = nameof(CardDocument.Id);

        var actual = DoRoundtripSerialization(expected);

        AssertCard(expected, actual);
        AssertDocument(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_Deck_Serialization()
    {
        var expected = GetDeck<Deck>();

        var actual = DoRoundtripSerialization(expected);

        AssertDeck(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_DeckDocument_Serialization()
    {
        var expected = GetDeck<DeckDocument>();
        expected.Id = nameof(DeckDocument.Id);

        var actual = DoRoundtripSerialization(expected);

        AssertDeck(expected, actual);
        AssertDocument(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_Meaning_Serialization()
    {
        var expected = GetMeaning();

        var actual = DoRoundtripSerialization(expected);

        Assert.AreEqual(expected.Example, actual.Example);
        Assert.AreEqual(expected.Hint, actual.Hint);
        Assert.AreEqual(expected.Text, actual.Text);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_Review_Serialization()
    {
        var expected = GetReview<Review>();

        var actual = DoRoundtripSerialization(expected);

        AssertReview(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_ReviewDocument_Serialization()
    {
        var expected = GetReview<ReviewDocument>();
        expected.Id = nameof(ReviewDocument.Id);

        var actual = DoRoundtripSerialization(expected);

        AssertReview(expected, actual);
        AssertDocument(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_PasswordHash_Serialization()
    {
        var expected = GetPasswordHash();

        var actual = DoRoundtripSerialization(expected);

        AssertPasswordHash(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_PersistentToken_Serialization()
    {
        var expected = GetPersistentToken();

        var actual = DoRoundtripSerialization(expected);

        AssertPersistentToken(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_User_Serialization()
    {
        var expected = GetUser<User>();

        var actual = DoRoundtripSerialization(expected);

        AssertUser(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_UserDocument_Serialization()
    {
        var expected = GetUser<UserDocument>();
        expected.Id = nameof(UserDocument.Id);
        expected.PasswordHash = GetPasswordHash();
        expected.PersistentTokens = new List<PersistentToken>() { GetPersistentToken() };

        var actual = DoRoundtripSerialization(expected);

        AssertUser(expected, actual);
        AssertDocument(expected, actual);
        AssertPasswordHash(expected.PasswordHash, actual.PasswordHash!);
        AssertPersistentToken(expected.PersistentTokens.Single(), actual.PersistentTokens!.Single());
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ClassMap_Verification_Serialization()
    {
        var expected = new Verification()
        {
            CreationDate = DateTime.UtcNow,
            EmailAddress = nameof(Verification.EmailAddress),
            ExpirationDate = DateTime.UtcNow,
            Id = nameof(Verification.Id),
            Salt = new byte[] { 0 },
            Type = VerificationType.PasswordReset,
            UserId = nameof(Verification.UserId),
        };

        var actual = DoRoundtripSerialization(expected);

        Assert.AreEqual(expected.CreationDate, actual.CreationDate);
        Assert.AreEqual(expected.EmailAddress, actual.EmailAddress);
        Assert.AreEqual(expected.ExpirationDate, actual.ExpirationDate);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.IsTrue(Enumerable.SequenceEqual(expected.Salt, actual.Salt));
        Assert.AreEqual(expected.Type, actual.Type);
        Assert.AreEqual(expected.UserId, actual.UserId);
    }

    private static void AssertCard<TCard>(TCard expected, TCard actual)
        where TCard : Card
    {
        var expectedMeaning = expected.Meanings.Single();
        var actualMeaning = actual.Meanings.Single();
        Assert.AreEqual(expected.CreationDate, actual.CreationDate);
        Assert.AreEqual(expected.Expression, actual.Expression);
        Assert.AreEqual(expectedMeaning.Example, actualMeaning.Example);
        Assert.AreEqual(expected.Notes, actual.Notes);
        Assert.AreEqual(expected.Reading, actual.Reading);
        Assert.AreEqual(expected.Tags.Single(), actual.Tags.Single());
    }

    private static void AssertDeck<TDeck>(TDeck expected, TDeck actual)
        where TDeck : Deck
    {
        Assert.AreEqual(expected.Cards.Single(), actual.Cards.Single());
        Assert.AreEqual(expected.CreationDate, actual.CreationDate);
        Assert.AreEqual(expected.Name, actual.Name);
        Assert.AreEqual(expected.UserId, actual.UserId);
    }

    private static void AssertDocument<TDocument>(TDocument expected, TDocument actual)
        where TDocument : IDocument
    {
        Assert.AreEqual(expected.Id, actual.Id);
    }

    private static void AssertPasswordHash(PasswordHash expected, PasswordHash actual)
    {
        Assert.AreEqual(expected.ExpirationDate, actual.ExpirationDate);
        Assert.IsTrue(Enumerable.SequenceEqual(expected.Hash, actual.Hash));
        Assert.AreEqual(expected.LastChangeDate, actual.LastChangeDate);
        Assert.IsTrue(Enumerable.SequenceEqual(expected.Salt, actual.Salt));
        Assert.AreEqual(expected.Version.Name, actual.Version.Name);
    }

    private static void AssertPersistentToken(PersistentToken expected, PersistentToken actual)
    {
        Assert.AreEqual(expected.ClientId, actual.ClientId);
        Assert.AreEqual(expected.ExpirationDate, actual.ExpirationDate);
        Assert.IsTrue(Enumerable.SequenceEqual(expected.Hash, actual.Hash));
    }

    private static void AssertReview<TReview>(TReview expected, TReview actual)
        where TReview : Review
    {
        Assert.AreEqual(expected.CardId, actual.CardId);
        Assert.AreEqual(expected.Disposition, actual.Disposition);
        Assert.AreEqual(expected.Interval, actual.Interval);
        Assert.AreEqual(expected.ReviewDate, actual.ReviewDate);
        Assert.AreEqual(expected.UserId, actual.UserId);
    }

    private static void AssertUser<TUser>(TUser expected, TUser actual)
        where TUser : User
    {
        Assert.AreEqual(expected.EmailAddress, actual.EmailAddress);
        Assert.AreEqual(expected.Name, actual.Name);
        Assert.AreEqual(expected.UserName, actual.UserName);
        Assert.AreEqual(expected.UserStatus, actual.UserStatus);
    }

    private static TDocument DoRoundtripSerialization<TDocument>(TDocument document)
    {
        var bsonDocument = new BsonDocument();
        using var bsonWriter = new BsonDocumentWriter(bsonDocument);
        BsonSerializer.Serialize(bsonWriter, document);
        using var bsonReader = new BsonDocumentReader(bsonDocument);

        return BsonSerializer.Deserialize<TDocument>(bsonReader);
    }

    private static TCard GetCard<TCard>()
        where TCard : Card, new()
    {
        return new TCard()
        {
            CreationDate = DateTime.UtcNow,
            Expression = nameof(Card.Expression),
            Meanings = new List<Meaning>()
            {
                GetMeaning(),
            },
            Notes = nameof(Card.Notes),
            Reading = nameof(Card.Reading),
            Tags = new List<string>()
            {
                "Tag",
            },
        };
    }

    private static TDeck GetDeck<TDeck>()
        where TDeck : Deck, new()
    {
        return new TDeck()
        {
            Cards = new List<string>() { "Card" },
            CreationDate = DateTime.UtcNow,
            Name = nameof(Deck.Name),
            UserId = nameof(Deck.UserId),
        };
    }

    private static Meaning GetMeaning()
    {
        return new Meaning()
        {
            Example = nameof(Meaning.Example),
            Hint = nameof(Meaning.Hint),
            Text = nameof(Meaning.Text),
        };
    }

    private static PasswordHash GetPasswordHash()
    {
        return new PasswordHash()
        {
            ExpirationDate = DateTime.UtcNow,
            Hash = new byte[] { 0 },
            LastChangeDate = DateTime.UtcNow,
            Salt = new byte[] { 0 },
            Version = PasswordHashVersion.Current,
        };
    }

    private static PersistentToken GetPersistentToken()
    {
        return new PersistentToken()
        {
            ClientId = nameof(PersistentToken.ClientId),
            ExpirationDate = DateTime.UtcNow,
            Hash = new byte[] { 0 },
        };
    }

    private static TReview GetReview<TReview>()
        where TReview : Review, new()
    {
        return new TReview()
        {
            CardId = nameof(Review.CardId),
            Disposition = Disposition.Remembered,
            Interval = Interval.Beginner2,
            ReviewDate = DateTime.UtcNow,
            UserId = nameof(Review.UserId),
        };
    }

    private static TUser GetUser<TUser>()
        where TUser : User, new()
    {
        return new TUser()
        {
            EmailAddress = nameof(User.EmailAddress),
            Name = nameof(User.Name),
            UserName = nameof(User.UserName),
        };
    }
}
