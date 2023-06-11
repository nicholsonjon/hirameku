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

using Newtonsoft.Json;

/// <summary>
/// This class captures the data elements necessary to verify a user's password.
/// </summary>
internal class PasswordHash
{
    public PasswordHash()
    {
        this.Hash = Array.Empty<byte>();
        this.Salt = Array.Empty<byte>();
        this.Version = PasswordHashVersion.Current;
    }

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the derived key (i.e. hash).
    /// </summary>
    /// <remarks>
    /// <see cref="Hash"/> is generated using PBKDF2, the user's password, and the parameters defined by
    /// <see cref="Version"/>. The generated <see cref="Salt"/> is stored with <see cref="Hash"/> in the credential
    /// store along with the <see cref="PasswordHashVersion.Name"/>. The user's password must never be stored or
    /// logged (neither cleartext nor ciphertext).
    /// </remarks>
    [JsonIgnore]
    public byte[] Hash { get; set; }

    /// <summary>
    /// Gets or sets the last change date.
    /// </summary>
    public DateTime LastChangeDate { get; set; }

    /// <summary>
    /// Gets or sets the salt.
    /// </summary>
    [JsonIgnore]
    public byte[] Salt { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="PasswordHashVersion"/>.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="PasswordHashVersion.Current"/> if not otherwise specified.
    /// </remarks>
    [JsonConverter(typeof(PasswordHashVersionConverter))]
    public PasswordHashVersion Version { get; set; }
}
