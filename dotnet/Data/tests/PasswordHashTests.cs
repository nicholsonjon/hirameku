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

using Newtonsoft.Json;

[TestClass]
public class PasswordHashTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHash_Constructor()
    {
        var target = new PasswordHash();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHash_ExpirationDate()
    {
        var expirationDate = DateTime.UtcNow;

        var target = new PasswordHash()
        {
            ExpirationDate = expirationDate,
        };

        Assert.AreEqual(expirationDate, target.ExpirationDate);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHash_Hash()
    {
        var hash = new byte[] { 0 };

        var target = new PasswordHash()
        {
            Hash = hash,
        };

        Assert.AreEqual(hash, target.Hash);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHash_JsonSerialization_HashIsIgnored()
    {
        var hash = new byte[] { 0 };

        var target = DoRoundtripSerialization(new PasswordHash() { Hash = hash });

        Assert.IsNotNull(target);
        Assert.AreEqual(0, target!.Hash.Length);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHash_JsonSerialization_SaltIsIgnored()
    {
        var salt = new byte[] { 0 };

        var target = DoRoundtripSerialization(new PasswordHash() { Salt = salt });

        Assert.IsNotNull(target);
        Assert.AreEqual(0, target!.Salt.Length);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHash_LastChangeDate()
    {
        var lastChangeDate = DateTime.UtcNow;

        var target = new PasswordHash()
        {
            LastChangeDate = lastChangeDate,
        };

        Assert.AreEqual(lastChangeDate, target.LastChangeDate);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHash_Salt()
    {
        var salt = new byte[] { 0 };

        var target = new PasswordHash()
        {
            Salt = salt,
        };

        Assert.AreEqual(salt, target.Salt);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHash_Version()
    {
        var version = PasswordHashVersion.Current;

        var target = new PasswordHash()
        {
            Version = version,
        };

        Assert.AreEqual(version, target.Version);
    }

    private static PasswordHash? DoRoundtripSerialization(PasswordHash passwordHash)
    {
        var serialized = JsonConvert.SerializeObject(passwordHash);

        return JsonConvert.DeserializeObject<PasswordHash>(serialized);
    }
}
