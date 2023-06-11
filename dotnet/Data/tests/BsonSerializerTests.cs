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

using Hirameku.TestTools;
using MongoDB.Driver;

[TestClass]
public class BsonSerializerTests
{
    [Ignore]
    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task TestSerialization()
    {
        var user = new UserDocument()
        {
            EmailAddress = "test@test.local",
            Name = "Jon",
            PasswordHash = new PasswordHash()
            {
                Hash = TestData.GetHMACSHA512HashedPasswordBytes(),
                LastChangeDate = DateTime.UtcNow,
                Salt = TestData.GetHMACSHA512SaltBytes(),
                Version = PasswordHashVersion.Current,
            },
            UserName = "root",
        };

        ClassMap.RegisterClassMaps();

        var collection = GetCollection();
        _ = await collection.DeleteOneAsync(u => u.Name == "Jon").ConfigureAwait(false);

        await collection.InsertOneAsync(user).ConfigureAwait(false);
    }

    private static IMongoCollection<UserDocument> GetCollection()
    {
        var settings = new MongoClientSettings()
        {
            Credential = MongoCredential.CreateCredential("Hirameku", "Hirameku_service", string.Empty),
        };
        var client = new MongoClient(settings);
        var database = client.GetDatabase("Hirameku");

        return database.GetCollection<UserDocument>("test");
    }
}
