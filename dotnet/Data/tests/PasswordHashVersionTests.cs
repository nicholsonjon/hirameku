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

using Microsoft.AspNetCore.Cryptography.KeyDerivation;

[TestClass]
public class PasswordHashVersionTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHashVersion_GetCurrent()
    {
        Assert.AreSame(nameof(PasswordHashVersion.HMACSHA512), PasswordHashVersion.Current.Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHashVersion_GetHmac5122022()
    {
        var hmac5122022 = PasswordHashVersion.HMACSHA512;

        Assert.AreEqual(KeyDerivationPrf.HMACSHA512, hmac5122022.KeyDerivationPrf);
        Assert.AreEqual(210000, hmac5122022.Iterations);
        Assert.AreEqual(64, hmac5122022.KeyLength);
        Assert.AreEqual(nameof(PasswordHashVersion.HMACSHA512), hmac5122022.Name);
        Assert.AreEqual(64, hmac5122022.SaltLength);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    public void PasswordHashVersion_GetVersion_NameIsInvalid_Throws()
    {
        _ = PasswordHashVersion.GetVersion(null!);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHashVersion_GetVersion()
    {
        var name = nameof(PasswordHashVersion.HMACSHA512);

        var version = PasswordHashVersion.GetVersion(name);

        Assert.AreEqual(name, version.Name);
    }
}
