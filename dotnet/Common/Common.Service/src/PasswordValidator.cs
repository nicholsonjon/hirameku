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

namespace Hirameku.Common.Service;

using Hirameku.Common;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using NLog;
using System.Text.RegularExpressions;

public class PasswordValidator : IPasswordValidator
{
    private static readonly IDictionary<string, int> CharacterSpaces = new Dictionary<string, int>()
    {
        { Regexes.Digits, 10 },
        { Regexes.LowerCaseLetters, 26 },

        // Determining the right size of the character space for non-ASCII characters is hard. Using the entire
        // Unicode character set is wrong because most likely the password is simply written in a language other
        // than English. Other than languages that use Chinese characters (or comparable iconographic writing
        // systems), the size of the character set is probably not vastly larger than ASCII, so to save ourselves
        // a ton of work making this more precise, we'll just use the same size as printable ASCII characters.
        { Regexes.NonAsciiCharacters, 94 },

        // The division between punctuation and symobols is somewhat arbitrary here. In general, punctuation is
        // commonly used in ordinary English writing, whereas symobols are typically only found in special contexts
        // or usage. The primary motivation for dividing these is to avoid disproportionately increasing the size of
        // the character space by the inclusion of a single non-alphanumeric character.
        { Regexes.Punctuation, 15 },
        { Regexes.Symbols, 19 },
        { Regexes.UpperCaseLetters, 26 },
    };

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public PasswordValidator(
        IOptions<PasswordValidatorOptions> options,
        AsyncLazy<IEnumerable<string>> passwordBlacklist)
    {
        this.Options = options;
        this.PasswordBlacklist = passwordBlacklist;
    }

    private IOptions<PasswordValidatorOptions> Options { get; }

    private AsyncLazy<IEnumerable<string>> PasswordBlacklist { get; }

    public async Task<PasswordValidationResult> Validate(
        string password,
        CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { password = "REDACTED" } });

        var options = this.Options.Value;
        PasswordValidationResult result;

        if (string.IsNullOrEmpty(password) || CalculateEntropy(password) < options.MinPasswordEntropy)
        {
            result = PasswordValidationResult.InsufficientEntropy;
        }
        else if (password.Length > options.MaxPasswordLength)
        {
            result = PasswordValidationResult.TooLong;
        }
        else
        {
            cancellationToken.ThrowIfCancellationRequested();

            var blacklist = await this.PasswordBlacklist.ConfigureAwait(false);

            result = blacklist.Contains(password)
                ? PasswordValidationResult.Blacklisted
                : PasswordValidationResult.Valid;
        }

        Log.Trace("Entering method", data: new { returnValue = new { password = "REDACTED" } });

        return result;
    }

    private static double CalculateEntropy(string password)
    {
        Log.Trace("Entering method", data: new { parameters = new { password = "REDACTED" } });

        double entropy;

        if (!string.IsNullOrEmpty(password))
        {
            var totalPossibleCharacters = 0;

            foreach (var characterSpace in CharacterSpaces)
            {
                if (Regex.IsMatch(password, characterSpace.Key))
                {
                    totalPossibleCharacters += characterSpace.Value;
                }
            }

            entropy = Math.Log2(Math.Pow(totalPossibleCharacters, password.Length));
        }
        else
        {
            entropy = 0d;
        }

        Log.Trace("Exiting method", data: new { returnValue = entropy });

        return entropy;
    }
}
