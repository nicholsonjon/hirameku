﻿// Hirameku is a cloud-native, vendor-agnostic, serverless application for
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

namespace Hirameku.Recaptcha;

using System.Text.Json.Serialization;

internal class RecaptchaRequest
{
    public RecaptchaRequest()
    {
    }

    [JsonPropertyName("remoteIp")]
    public string RemoteIP { get; set; } = string.Empty;

    public string Response { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;
}
