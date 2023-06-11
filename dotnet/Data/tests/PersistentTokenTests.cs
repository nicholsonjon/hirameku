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

[TestClass]
public class PersistentTokenTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PersistentToken_Constructor()
    {
        var target = new PersistentToken();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PersistentToken_ClientId()
    {
        const string ClientId = nameof(ClientId);

        var target = new PersistentToken()
        {
            ClientId = ClientId,
        };

        Assert.AreEqual(ClientId, target.ClientId);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PersistentToken_ExpirationDate()
    {
        var expirationDate = DateTime.UtcNow;

        var target = new PersistentToken()
        {
            ExpirationDate = expirationDate,
        };

        Assert.AreEqual(expirationDate, target.ExpirationDate);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PersistentToken_Hash()
    {
        var hash = new byte[] { 0 };

        var target = new PersistentToken()
        {
            Hash = hash,
        };

        Assert.AreSame(hash, target.Hash);
    }
}
