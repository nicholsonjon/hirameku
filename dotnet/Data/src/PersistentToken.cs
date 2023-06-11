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

/// <summary>
/// This class captures the data elements necessary to verify a persistent login token from a client device.
/// </summary>
internal class PersistentToken
{
    public PersistentToken()
    {
    }

    /// <summary>
    /// Gets or sets the unique id of the client device (i.e. the user agent).
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public DateTime ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the derived key (i.e. hash).
    /// </summary>
    /// <remarks>
    /// The <see cref="Hash"/> is generated using <see cref="PasswordHashVersion.Current"/>, with
    /// <see cref="ClientId"/> and the unique token generated for the client serving as the passphrase, and the
    /// user's <see cref="PasswordHash.Hash"/> serving as the salt. This has the desirable effect of automatically
    /// invalidating all tokens whenever the user's password changes.
    /// </remarks>
    public byte[] Hash { get; set; } = Array.Empty<byte>();
}
