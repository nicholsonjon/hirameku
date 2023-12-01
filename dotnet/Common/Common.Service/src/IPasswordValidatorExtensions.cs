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

using FluentValidation;
using Hirameku.Common.Service.Properties;
using System.Globalization;
using System.Text;

public static class IPasswordValidatorExtensions
{
    public static async Task ValidateAsync<T>(
        this IPasswordValidator instance,
        string password,
        ValidationContext<T> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(context);

        var result = await instance.Validate(password, cancellationToken).ConfigureAwait(false);

        if (result != PasswordValidationResult.Valid)
        {
            var message = result switch
            {
                PasswordValidationResult.Blacklisted => Exceptions.PasswordBlacklisted,
                PasswordValidationResult.InsufficientEntropy => Exceptions.InsufficientPasswordEntropy,
                PasswordValidationResult.TooLong => Exceptions.PasswordTooLong,
                _ => throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    CompositeFormat.Parse(Exceptions.InvalidEnumValue).Format,
                    result,
                    typeof(PasswordValidationResult))),
            };

            context.AddFailure(message);
        }
    }
}
