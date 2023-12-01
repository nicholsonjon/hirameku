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
using System.Security.Cryptography;
using System.Text;

[TestClass]
public class VerificationTokenTests
{
    private const string EmailAddress = nameof(EmailAddress);
    private const int SaltAndPepperLength = 16;
    private static readonly DateTime CreationDate = DateTime.UtcNow;
    private static readonly HashAlgorithmName HashName = HashAlgorithmName.SHA256;

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task VerificationToken_Create()
    {
        var expirationDate = CreationDate + TimeSpan.FromHours(1);
        var verification = new Verification
        {
            CreationDate = CreationDate,
            EmailAddress = EmailAddress,
            ExpirationDate = expirationDate,
            Salt = RandomNumberGenerator.GetBytes(SaltAndPepperLength),
        };
        var pepper = RandomNumberGenerator.GetBytes(SaltAndPepperLength);
        var token = await VerificationToken.Create(verification, pepper, HashName).ConfigureAwait(false);

        Assert.IsNotNull(token);
        Assert.AreEqual(token.EmailAddress, EmailAddress);
        Assert.AreEqual(token.ExpirationDate, verification.ExpirationDate);
        Assert.AreEqual(token.Pepper, Convert.ToBase64String(pepper));
        Assert.AreEqual(token.Token, await ComputeHash(verification.Salt, pepper).ConfigureAwait(false));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task VerificationToken_JsonSerialization_PepperIsIgnored()
    {
        var target = await VerificationToken.Create(
            new Verification(),
            new byte[] { 0 },
            HashName)
            .ConfigureAwait(false);

        var serialized = JsonConvert.SerializeObject(target);
        var deserialized = JsonConvert.DeserializeObject<VerificationToken>(serialized);

        Assert.IsTrue(string.IsNullOrEmpty(deserialized?.Pepper));
    }

    private static async Task<string> ComputeHash(byte[] salt, byte[] pepper)
    {
        var creationDate = BitConverter.GetBytes(CreationDate.ToBinary());
        var emailBytes = Encoding.UTF8.GetBytes(EmailAddress);
        using var stream = new MemoryStream(
            creationDate.Length + EmailAddress.Length + salt.Length + pepper.Length);

        await stream.WriteAsync(emailBytes).ConfigureAwait(false);
        await stream.WriteAsync(creationDate).ConfigureAwait(false);
        await stream.WriteAsync(salt).ConfigureAwait(false);
        await stream.WriteAsync(pepper).ConfigureAwait(false);

        _ = stream.Seek(0, SeekOrigin.Begin);

        using var hashAlgorithm = CryptoConfig.CreateFromName(HashName.Name!) as HashAlgorithm;
        var hash = await hashAlgorithm!.ComputeHashAsync(stream).ConfigureAwait(false);

        return Convert.ToBase64String(hash);
    }
}
