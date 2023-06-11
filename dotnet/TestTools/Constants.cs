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

namespace Hirameku.TestTools;

using CommonConstants = Hirameku.Common.Constants;

public static class Constants
{
    public const int InvalidIdLength = ValidIdLength + 1;
    public const int InvalidNumberOfCards = CommonConstants.MaxNumberOfCards + 1;
    public const int InvalidNumberOfMeanings = CommonConstants.MaxNumberOfMeanings + 1;
    public const int InvalidLongLength = CommonConstants.MaxStringLengthLong + 1;
    public const int InvalidShortLength = CommonConstants.MaxStringLengthShort + 1;
    public const int MaxUserNameLength = 32;
    public const int MinUserNameLength = 4;

    // 24 because a MongoDB ObjectId represented as a hexadecimal string is 24 characters
    public const int ValidIdLength = 24;
}
