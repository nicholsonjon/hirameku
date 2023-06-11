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
public class HashPasswordResultTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void HashPasswordResult_Constructor()
    {
        var target = new HashPasswordResult(
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            PasswordHashVersion.Current);

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void HashPasswordResult_GetHash()
    {
        var hash = new byte[] { 0 };

        var target = new HashPasswordResult(hash, Array.Empty<byte>(), PasswordHashVersion.Current);

        Assert.AreEqual(hash, target.Hash);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void HashPasswordResult_GetSalt()
    {
        var salt = new byte[] { 0 };

        var target = new HashPasswordResult(Array.Empty<byte>(), salt, PasswordHashVersion.Current);

        Assert.AreEqual(salt, target.Salt);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void HashPasswordResult_GetVersion()
    {
        var version = PasswordHashVersion.Current;

        var target = new HashPasswordResult(Array.Empty<byte>(), Array.Empty<byte>(), version);

        Assert.AreEqual(version, target.Version);
    }
}
