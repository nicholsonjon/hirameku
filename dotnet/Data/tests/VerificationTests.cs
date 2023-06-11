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
public class VerificationTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Verification_Constructor()
    {
        var target = new Verification();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Verification_CreationDate()
    {
        var creationDate = DateTime.UtcNow;

        var target = new Verification()
        {
            CreationDate = creationDate,
        };

        Assert.AreEqual(creationDate, target.CreationDate);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Verification_EmailAddress()
    {
        const string EmailAddress = nameof(EmailAddress);

        var target = new Verification()
        {
            EmailAddress = EmailAddress,
        };

        Assert.AreEqual(EmailAddress, target.EmailAddress);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Verification_ExpirationDate()
    {
        var expirationDate = DateTime.UtcNow;

        var target = new Verification()
        {
            ExpirationDate = expirationDate,
        };

        Assert.AreEqual(expirationDate, target.ExpirationDate);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Verification_Id()
    {
        const string Id = nameof(Id);

        var target = new Verification()
        {
            Id = Id,
        };

        Assert.AreEqual(Id, target.Id);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Verification_JsonSerialization_SaltIsIgnored()
    {
        var verification = new Verification()
        {
            Salt = new byte[] { 0 },
        };

        var serialized = JsonConvert.SerializeObject(verification);
        var target = JsonConvert.DeserializeObject<Verification>(serialized);

        Assert.AreEqual(Array.Empty<byte>(), target?.Salt);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Verification_Salt()
    {
        var salt = new byte[] { 0 };

        var target = new Verification()
        {
            Salt = salt,
        };

        Assert.AreEqual(salt, target.Salt);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Verification_Type()
    {
        var type = VerificationType.PasswordReset;

        var target = new Verification()
        {
            Type = type,
        };

        Assert.AreEqual(type, target.Type);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Verification_UserId()
    {
        const string UserId = nameof(UserId);

        var target = new Verification()
        {
            UserId = UserId,
        };

        Assert.AreEqual(UserId, target.UserId);
    }
}
