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
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Globalization;
using System.Text;

/// <summary>
/// This class represents a particular version of the PBKDF2 function, defining the parameters used to generate the
/// derived key (i.e. the password hash).
/// </summary>
public class PasswordHashVersion
{
    private PasswordHashVersion()
    {
    }

    public static PasswordHashVersion Current => HMACSHA512;

    /// <summary>
    /// Gets the first version of the HMACSHA512 <see cref="PasswordHashVersion"/>, which was adopted spring of 2023.
    /// </summary>
    /// <remarks>
    /// This is a PBKDF2-based hash using a 512-bit salt, 210,000 iterations, and HMACSHA512, producing a 512-bit
    /// derived key (i.e. hash). These are based partly on OWASP's currently recommended parameters.
    /// https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html#pbkdf2.
    /// </remarks>
    public static PasswordHashVersion HMACSHA512 => new()
    {
        Iterations = 210000,
        KeyDerivationPrf = KeyDerivationPrf.HMACSHA512,
        KeyLength = 64,
        Name = nameof(HMACSHA512),
        SaltLength = 64,
    };

    /// <summary>
    /// Gets the a <see cref="PasswordHashVersion"/> with parameters matching those of
    /// <see cref="Microsoft.Extensions.Identity.Core.PasswordHasher{TUser}"/> using
    /// <see cref="Microsoft.Extensions.Identity.Core.PasswordHasherCompatibilityMode.IdentityV3"/>.
    /// </summary>
    /// <remarks>
    /// This implementation does not conform to OWASP recommendations as of year 2022, and is marked obsolete.
    /// </remarks>
    [Obsolete("This implementation does not conform to OWASP recommendations as of year 2022")]
    public static PasswordHashVersion IdentityV3 => new()
    {
        Iterations = 100000,
        KeyDerivationPrf = KeyDerivationPrf.HMACSHA256,
        KeyLength = 32,
        Name = nameof(IdentityV3),
        SaltLength = 16,
    };

    /// <summary>
    /// Gets the number of iterations used to produce the derived key.
    /// </summary>
    public int Iterations { get; init; }

    /// <summary>
    /// Gets the <see cref="KeyDerivationPrf"/> used to produce the derived key.
    /// </summary>
    public KeyDerivationPrf KeyDerivationPrf { get; init; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <remarks>
    /// This is the only field serialized and persisted in the credential store, so it must be unique for each version.
    /// </remarks>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the length of the derived key (i.e. the hash) in bytes.
    /// </summary>
    public int KeyLength { get; init; }

    /// <summary>
    /// Gets the length of the salt in bytes.
    /// </summary>
    public int SaltLength { get; init; }

    public static PasswordHashVersion GetVersion(string name)
    {
        PasswordHashVersion version;

        switch (name)
        {
            case nameof(HMACSHA512):

                version = HMACSHA512;
                break;

#pragma warning disable CS0618 // Type or member is obsolete

            case nameof(IdentityV3):

                version = IdentityV3;
                break;

#pragma warning restore CS0618 // Type or member is obsolete

            default:

                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    CompositeFormat.Parse(Exceptions.InvalidPasswordHashVersion).Format,
                    name);

                throw new ArgumentException(message, nameof(name));
        }

        return version;
    }

    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}",
            this.GetType().FullName,
            this.Name);
    }
}
