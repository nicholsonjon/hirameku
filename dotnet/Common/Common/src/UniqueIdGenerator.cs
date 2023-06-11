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

using SimpleBase;

public class UniqueIdGenerator : IUniqueIdGenerator
{
    public UniqueIdGenerator(IGuidProvider guidProvider)
    {
        this.GuidProvider = guidProvider;
    }

    private IGuidProvider GuidProvider { get; }

    public string GenerateUniqueId()
    {
        var guid = this.GuidProvider.GenerateGuid();
        return Base85.Z85.Encode(guid.ToByteArray());
    }
}
