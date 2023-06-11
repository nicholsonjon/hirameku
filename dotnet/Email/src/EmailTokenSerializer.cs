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

namespace Hirameku.Email;

using Hirameku.Common;
using Hirameku.Email.Properties;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

public class EmailTokenSerializer : IEmailTokenSerializer
{
    public EmailTokenSerializer(IOptions<VerificationOptions> options)
    {
        this.Options = options;
    }

    private IOptions<VerificationOptions> Options { get; }

    public Tuple<string, string, string> Deserialize(string serializedToken)
    {
        byte[] bytes;

        try
        {
            bytes = Convert.FromBase64String(serializedToken);
        }
        catch (Exception ex)
        {
            throw new InvalidTokenException(Exceptions.InvalidToken, ex);
        }

        var options = this.Options.Value;
        var hashName = options.HashName;
        var pepperLength = options.PepperLength;
        int tokenLength;

        try
        {
            using var hashAlgorithm = HashAlgorithm.Create(hashName.Name!);
            tokenLength = hashAlgorithm!.HashSize / 8;
        }
        catch (Exception ex)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                Exceptions.InvalidHashAlgorithm,
                hashName.Name);

            throw new InvalidOperationException(message, ex);
        }

        if (bytes.Length <= pepperLength + tokenLength + 1)
        {
            throw new InvalidTokenException(Exceptions.InvalidToken);
        }

        var pepperBytes = new byte[pepperLength];
        var tokenBytes = new byte[tokenLength];
        var userNameBytes = new byte[bytes.Length - pepperLength - tokenLength];

        Buffer.BlockCopy(bytes, 0, pepperBytes, 0, pepperLength);
        Buffer.BlockCopy(bytes, pepperLength, tokenBytes, 0, tokenLength);
        Buffer.BlockCopy(bytes, pepperLength + tokenLength, userNameBytes, 0, userNameBytes.Length);

        return new Tuple<string, string, string>(
           Convert.ToBase64String(pepperBytes),
           Convert.ToBase64String(tokenBytes),
           Encoding.UTF8.GetString(userNameBytes));
    }

    public string Serialize(string pepper, string token, string userName)
    {
        var pepperBytes = Convert.FromBase64String(pepper);
        var tokenBytes = Convert.FromBase64String(token);
        var userNameBytes = Convert.FromBase64String(userName);
        var bytes = new byte[pepperBytes.Length + tokenBytes.Length + userNameBytes.Length];
        var pepperLength = pepperBytes.Length;
        var tokenLength = tokenBytes.Length;

        Buffer.BlockCopy(pepperBytes, 0, bytes, 0, pepperLength);
        Buffer.BlockCopy(tokenBytes, 0, bytes, pepperLength, tokenLength);
        Buffer.BlockCopy(userNameBytes, 0, bytes, pepperLength + tokenLength, userNameBytes.Length);

        return Convert.ToBase64String(bytes);
    }
}
