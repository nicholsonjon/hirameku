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

using Bogus;
using System.Text;

public static class TestData
{
    public const string HMACSHA512HashedPassword = "MC5Uoyiz9kA2kNltDalZHIHfWXmKTG0NlMzq6celrM8ijLMxP+uA43hGpqZ8m31v/i2ij0E1srpjyJ3IpCp+bg==";
    public const string HMACSHA512Salt = "5R1sOVb3tPoi+8cI+QSv5xMvWnVsQtIQ9sad81yYP95g4jdkaM2nbNAQHi7AuipaniZZNFLkJsYjrHqo1T/VnQ==";
    public const string IdentityV3HashedPassword = "8yUMIXoKqVgu9YyomPUbazfjQZD7kqEkzAkrq4RFPys=";
    public const string IdentityV3Salt = "vXjCme/AHNThLhNalPw0OQ==";
    public const string Password = "ZCP,WF6'*k)z>$[L~s]m";
    public const string Pepper = "UGVwcGVy";
    public const string SerializedToken = "U2VyaWFsaXplZFRva2Vu";
    public const string Token = "VG9rZW4=";

    public static byte[] GetHMACSHA512HashedPasswordBytes()
    {
        return Convert.FromBase64String(HMACSHA512HashedPassword);
    }

    public static byte[] GetHMACSHA512SaltBytes()
    {
        return Convert.FromBase64String(HMACSHA512Salt);
    }

    public static byte[] GetIdentityV3HashedPasswordBytes()
    {
        return Convert.FromBase64String(IdentityV3HashedPassword);
    }

    public static byte[] GetIdentityV3SaltBytes()
    {
        return Convert.FromBase64String(IdentityV3Salt);
    }

    public static string GetRandomUserName(int minLength = 4, int maxLength = 32)
    {
        if (minLength < 1 || maxLength < 1)
        {
            return string.Empty;
        }
        else if (minLength > maxLength)
        {
            maxLength = minLength;
        }

        var minLengthQuotient = minLength / 3;
        var minLengthRemainder = minLength % 3;
        var maxLengthQuotient = maxLength / 3;
        var maxLengthRemainder = maxLength % 3;

        var userName = new StringBuilder(maxLength);
        var random = new Faker().Random;
        _ = userName.Append(random.String(
            minLengthQuotient + minLengthRemainder,
            maxLengthQuotient + maxLengthRemainder,
            'A',
            'Z'));
        _ = userName.Append(random.String(minLengthQuotient, maxLengthQuotient, 'a', 'z'));
        _ = userName.Append(random.String2(minLengthQuotient, maxLengthQuotient, "0123456789-._~!*"));

        return userName.ToString();
    }
}
