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

namespace Hirameku.Common;

using Hirameku.Common.Properties;
using Newtonsoft.Json;
using NLog;
using NLog.Common;
using System.Globalization;
using System.IO;
using System.Text;

public class JsonLogSerializer : IJsonConverter
{
    public JsonLogSerializer(JsonSerializerSettings settings)
    {
        this.Settings = settings;
    }

    private JsonSerializerSettings Settings { get; }

    public bool SerializeObject(object value, StringBuilder builder)
    {
        try
        {
            var serializer = JsonSerializer.CreateDefault(this.Settings);
            using var stringWriter = new StringWriter(builder, CultureInfo.InvariantCulture);
            using var jsonWriter = new JsonTextWriter(stringWriter);
            jsonWriter.Formatting = serializer.Formatting;
            serializer.Serialize(jsonWriter, value);
        }
        catch (Exception ex)
        {
            InternalLogger.Error(ex, Exceptions.SerializationError);
            throw;
        }

        return true;
    }
}
