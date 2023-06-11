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
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

[TestClass]
public class PasswordHasherTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHasher_Constructor()
    {
        var target = new PasswordHasher();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHasher_CurrentVersion_Constructor()
    {
        var target = new PasswordHasher(PasswordHashVersion.HMACSHA512);

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHasher_HashPassword_PasswordAndSalt()
    {
        var version = PasswordHashVersion.HMACSHA512;
        var target = new PasswordHasher(version);

        var result = target.HashPassword(TestData.Password, TestData.GetHMACSHA512SaltBytes());

        Assert.IsNotNull(result);
        Assert.AreEqual(version.KeyLength, result.Hash.Length);
        Assert.AreEqual(version.SaltLength, result.Salt.Length);
        Assert.AreSame(version, result.Version);

        var hash = KeyDerivation.Pbkdf2(
            TestData.Password,
            result.Salt,
            version.KeyDerivationPrf,
            version.Iterations,
            version.KeyLength);

        Assert.IsTrue(hash.SequenceEqual(result.Hash));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHasher_HashPassword_PasswordAndVersion()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        // we use IdentityV3 for this test because HMACSHA512 is the default
        var version = PasswordHashVersion.IdentityV3;
#pragma warning restore CS0618 // Type or member is obsolete

        var target = new PasswordHasher();

        var result = target.HashPassword(TestData.Password, version);

        Assert.IsNotNull(result);
        Assert.AreEqual(version.KeyLength, result.Hash.Length);
        Assert.AreEqual(version.SaltLength, result.Salt.Length);
        Assert.AreSame(version, result.Version);

        var hash = KeyDerivation.Pbkdf2(
            TestData.Password,
            result.Salt,
            version.KeyDerivationPrf,
            version.Iterations,
            version.KeyLength);

        Assert.IsTrue(hash.SequenceEqual(result.Hash));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public void PasswordHasher_HashPassword_PasswordAndVersion_VersionIsNull_Throws()
    {
        var target = new PasswordHasher(PasswordHashVersion.Current);

        _ = target.HashPassword(TestData.Password, (PasswordHashVersion)null!);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHasher_HashPassword_PasswordSaltAndVersion()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        // we use IdentityV3 for this test because HMACSHA512 is the default
        var version = PasswordHashVersion.IdentityV3;
#pragma warning restore CS0618 // Type or member is obsolete

        var target = new PasswordHasher();

        var result = target.HashPassword(TestData.Password, TestData.GetIdentityV3SaltBytes(), version);

        Assert.IsNotNull(result);
        Assert.AreEqual(version.KeyLength, result.Hash.Length);
        Assert.AreEqual(version.SaltLength, result.Salt.Length);
        Assert.AreSame(version, result.Version);

        var hash = KeyDerivation.Pbkdf2(
            TestData.Password,
            result.Salt,
            version.KeyDerivationPrf,
            version.Iterations,
            version.KeyLength);

        Assert.IsTrue(hash.SequenceEqual(result.Hash));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(PasswordHasher_HashPassword_PasswordSaltAndVersion_Password_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(PasswordHasher_HashPassword_PasswordSaltAndVersion_Password_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(PasswordHasher_HashPassword_PasswordSaltAndVersion_Password_Throws) + "(WhiteSpace)")]
    public void PasswordHasher_HashPassword_PasswordSaltAndVersion_Password_Throws(string password)
    {
        var target = new PasswordHasher(PasswordHashVersion.Current);

        _ = target.HashPassword(password);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public void PasswordHash_HashPassword_PasswordSaltAndVersion_SaltIsNull_Throws()
    {
        var target = new PasswordHasher(PasswordHashVersion.Current);

        _ = target.HashPassword(TestData.Password, (byte[])null!);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public void PasswordHash_HashPassword_PasswordSaltAndVersion_VersionIsNull_Throws()
    {
        var target = new PasswordHasher(PasswordHashVersion.Current);

        _ = target.HashPassword(TestData.Password, new byte[] { 0 }, null!);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHasher_HashPassword_WithHMACSHA512()
    {
        var version = PasswordHashVersion.HMACSHA512;
        var target = new PasswordHasher(version);

        var result = target.HashPassword(TestData.Password);

        Assert.IsNotNull(result);
        Assert.AreEqual(version.KeyLength, result.Hash.Length);
        Assert.AreEqual(version.SaltLength, result.Salt.Length);
        Assert.AreSame(version, result.Version);

        var hash = KeyDerivation.Pbkdf2(
            TestData.Password,
            result.Salt,
            version.KeyDerivationPrf,
            version.Iterations,
            version.KeyLength);

        Assert.IsTrue(hash.SequenceEqual(result.Hash));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHasher_HashPassword_WithIdentityV3()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var version = PasswordHashVersion.IdentityV3;
#pragma warning restore CS0618 // Type or member is obsolete

        var target = new PasswordHasher(version);

        var result = target.HashPassword(TestData.Password);

        Assert.IsNotNull(result);
        Assert.AreEqual(version.KeyLength, result.Hash.Length);
        Assert.AreEqual(version.SaltLength, result.Salt.Length);
        Assert.AreSame(version, result.Version);

        var hash = KeyDerivation.Pbkdf2(
            TestData.Password,
            result.Salt,
            version.KeyDerivationPrf,
            version.Iterations,
            version.KeyLength);

        Assert.IsTrue(hash.SequenceEqual(result.Hash));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHasher_VerifyPassword_NotVerified()
    {
        var target = new PasswordHasher();

        var result = target.VerifyPassword(
            PasswordHashVersion.HMACSHA512,
            TestData.GetHMACSHA512SaltBytes(),
            TestData.GetHMACSHA512HashedPasswordBytes(),
            "This is not the right password");

        Assert.AreEqual(VerifyPasswordResult.NotVerified, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHasher_VerifyPassword_Verified()
    {
        var target = new PasswordHasher();

        var result = target.VerifyPassword(
            PasswordHashVersion.HMACSHA512,
            TestData.GetHMACSHA512SaltBytes(),
            TestData.GetHMACSHA512HashedPasswordBytes(),
            TestData.Password);

        Assert.AreEqual(VerifyPasswordResult.Verified, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHasher_VerifyPassword_VerifiedAndRehashRequired()
    {
        var target = new PasswordHasher();

#pragma warning disable CS0618 // Type or member is obsolete
        var result = target.VerifyPassword(
            PasswordHashVersion.IdentityV3,
            TestData.GetIdentityV3SaltBytes(),
            TestData.GetIdentityV3HashedPasswordBytes(),
            TestData.Password);
#pragma warning restore CS0618 // Type or member is obsolete

        Assert.AreEqual(VerifyPasswordResult.VerifiedAndRehashRequired, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public void PasswordHasher_VerifyPassword_VersionIsNull_Throws()
    {
        var target = new PasswordHasher(PasswordHashVersion.Current);

        _ = target.VerifyPassword(null!, Array.Empty<byte>(), Array.Empty<byte>(), string.Empty);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }
}
