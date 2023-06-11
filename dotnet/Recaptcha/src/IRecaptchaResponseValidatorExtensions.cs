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

namespace Hirameku.Recaptcha;

using Hirameku.Common;
using Hirameku.Recaptcha.Properties;
using NLog;
using System.Globalization;

public static class IRecaptchaResponseValidatorExtensions
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static async Task ValidateAndThrow(
        this IRecaptchaResponseValidator instance,
        string recaptchaResponse,
        string hostname,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new { instance, recaptchaResponse, hostname, action, remoteIP, cancellationToken },
            });

        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var result = await instance.Validate(recaptchaResponse, hostname, action, remoteIP, cancellationToken)
            .ConfigureAwait(false);

        if (result != RecaptchaVerificationResult.Verified)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                Exceptions.RecaptchaVerificationFailed,
                result);

            throw new RecaptchaVerificationFailedException(message);
        }

        Log.Trace("Exiting method", data: default(object));
    }
}
