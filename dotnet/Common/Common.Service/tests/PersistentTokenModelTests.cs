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

namespace Hirameku.Common.Service.Tests;

using Newtonsoft.Json;

[TestClass]
public class PersistentTokenModelTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PersistentTokenModel_Constructor()
    {
        var target = new PersistentTokenModel();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PersistentTokenModel_ClientId()
    {
        const string ClientId = nameof(ClientId);

        var target = new PersistentTokenModel() { ClientId = ClientId };

        Assert.AreEqual(ClientId, target.ClientId);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PersistentTokenModel_ClientToken()
    {
        const string ClientToken = nameof(ClientToken);

        var target = new PersistentTokenModel() { ClientToken = ClientToken };

        Assert.AreEqual(ClientToken, target.ClientToken);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PersistentTokenModel_ExpirationDate()
    {
        var expirationDate = DateTime.UtcNow;

        var target = new PersistentTokenModel() { ExpirationDate = expirationDate };

        Assert.AreEqual(expirationDate, target.ExpirationDate);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PersistentTokenModel_JsonSerialization_ClientTokenIsIgnored()
    {
        const string ClientToken = nameof(ClientToken);
        var target = new PersistentTokenModel() { ClientToken = ClientToken };

        var serialized = JsonConvert.SerializeObject(target);
        var deserialized = JsonConvert.DeserializeObject<PersistentTokenModel>(serialized);

        Assert.IsNotNull(deserialized);
        Assert.IsTrue(string.IsNullOrEmpty(deserialized!.ClientToken));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PersistentTokenModel_UserId()
    {
        const string UserId = nameof(UserId);

        var target = new PersistentTokenModel() { UserId = UserId };

        Assert.AreEqual(UserId, target.UserId);
    }
}
