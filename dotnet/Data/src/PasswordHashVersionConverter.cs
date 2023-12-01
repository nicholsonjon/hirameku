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
using System;
using System.Globalization;
using System.Text;

public class PasswordHashVersionConverter : JsonConverter<PasswordHashVersion>
{
    public PasswordHashVersionConverter()
    {
    }

    public override PasswordHashVersion? ReadJson(
        JsonReader reader,
        Type objectType,
        PasswordHashVersion? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var json = reader.Value;
        PasswordHashVersion result;

        if (json is string version && !string.IsNullOrWhiteSpace(version))
        {
            result = PasswordHashVersion.GetVersion(version);
        }
        else
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                CompositeFormat.Parse(Exceptions.InvalidPasswordHashVersionRepresentation).Format,
                json);

            throw new JsonSerializationException(message);
        }

        return result;
    }

    public override void WriteJson(JsonWriter writer, PasswordHashVersion? value, JsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (value != null)
        {
            writer.WriteValue(value.Name);
        }
    }
}
