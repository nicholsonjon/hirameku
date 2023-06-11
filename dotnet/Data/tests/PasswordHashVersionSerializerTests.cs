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

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Moq;

[TestClass]
public class PasswordHashVersionSerializerTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHashVersionSerializer_Constructor()
    {
        var target = new PasswordHashVersionSerializer();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHashVersionSerializer_Deserialize()
    {
        var name = PasswordHashVersion.HMACSHA512.Name;
        var mockReader = new Mock<IBsonReader>();
        _ = mockReader.Setup(m => m.ReadString())
            .Returns(name);
        var target = new PasswordHashVersionSerializer();

        var version = target.Deserialize(BsonDeserializationContext.CreateRoot(mockReader.Object), default);

        Assert.AreEqual(name, version.Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHashVersionSerializer_Serialize()
    {
        var version = PasswordHashVersion.HMACSHA512;
        var mockWriter = new Mock<IBsonWriter>();
        _ = mockWriter.Setup(m => m.WriteString(version.Name));
        var target = new PasswordHashVersionSerializer();

        target.Serialize(BsonSerializationContext.CreateRoot(mockWriter.Object), default, version);

        mockWriter.VerifyAll();
    }
}
