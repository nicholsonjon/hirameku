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

namespace Hirameku.Data;

using Hirameku.Data.Properties;
using Newtonsoft.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

public class VerificationToken
{
    private VerificationToken()
    {
    }

    public string EmailAddress { get; init; } = string.Empty;

    public DateTime? ExpirationDate { get; init; }

    [JsonIgnore]
    public string Pepper { get; init; } = string.Empty;

    public string Token { get; init; } = string.Empty;

    internal static async Task<VerificationToken> Create(
        Verification verification,
        byte[] pepper,
        HashAlgorithmName hashAlgorithmName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verification);
        ArgumentNullException.ThrowIfNull(pepper);

        using var hashAlgorithm = CryptoConfig.CreateFromName(hashAlgorithmName.Name!) as HashAlgorithm;

        if (hashAlgorithm == null)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                CompositeFormat.Parse(Exceptions.InvalidHashAlgorithm).Format,
                hashAlgorithmName);

            throw new InvalidOperationException(message);
        }

        var creationDate = BitConverter.GetBytes(verification.CreationDate.ToBinary());
        var emailAddress = verification.EmailAddress;
        var emailBytes = Encoding.UTF8.GetBytes(emailAddress);
        var salt = verification.Salt;
        using var stream = new MemoryStream(
            creationDate.Length + emailAddress.Length + salt.Length + pepper.Length);

        await stream.WriteAsync(emailBytes, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(creationDate, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(salt, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(pepper, cancellationToken).ConfigureAwait(false);

        _ = stream.Seek(0, SeekOrigin.Begin);

        var hash = await hashAlgorithm.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);

        return new VerificationToken()
        {
            EmailAddress = emailAddress,
            ExpirationDate = verification.ExpirationDate,
            Pepper = Convert.ToBase64String(pepper),
            Token = Convert.ToBase64String(hash),
        };
    }
}
