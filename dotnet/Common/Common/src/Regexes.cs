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

public static class Regexes
{
    public const string Base64String = @"^(?:[0-9A-Za-z+/]{4})*(?:[0-9A-Za-z+/]{2}==|[0-9A-Za-z+/]{3}=)?$";
    public const string Digits = @"[0-9]+";
    public const string EmailAddress = @"^[0-9A-Za-z.!#$%&'*+\/=?^_`{|}~-]+@(?!-)(?:(?:[a-zA-Z0-9][a-zA-Z0-9\-]{0,61})?[a-zA-Z0-9]\.){1,126}(?!0-9+)[a-zA-Z0-9]{2,63}$";
    public const string EntirelyWhiteSpace = @"^\s+$";
    public const string HtmlTag = @"<[^>]+>";
    public const string LowerCaseLetters = @"[a-z]+";
    public const string Name = @"\p{L}+(?:\s*\p{L}+){1,40}";
    public const string NonAsciiCharacters = @"[^\u0000-\u007F]+";
    public const string ObjectId = @"^[0-9a-f]{24}$";
    public const string Punctuation = @"[ !""'(),\-./:;?`]+";
    public const string Symbols = @"[#$%&*+<=>@\[\\\]\^_{|}~]+";
    public const string UpperCaseLetters = @"[A-Z]+";

    // this regex features only characters valid for use in a URL that aren't reserved or unwise to use
    public const string UserName = @"^[0-9A-Za-z\-._~!*]{4,32}$";
    public const string Z85EncodedString = @"^[0-9A-Za-z.\-:+=^!/*?&<>()[\]{}@%$#]+$";
}
