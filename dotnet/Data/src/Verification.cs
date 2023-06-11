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
using System.Diagnostics.CodeAnalysis;

public class Verification : IDocument
{
    public const string CollectionName = "verifications";

    public Verification()
    {
    }

    public DateTime CreationDate { get; set; }

    public string EmailAddress { get; set; } = string.Empty;

    public DateTime? ExpirationDate { get; set; }

    public string Id { get; set; } = string.Empty;

    [JsonIgnore]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "This is a DTO")]
    public byte[] Salt { get; set; } = Array.Empty<byte>();

    public VerificationType Type { get; set; }

    public string UserId { get; set; } = string.Empty;
}
