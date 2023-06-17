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
using Hirameku.Common.Properties;
using NLog;
using System.Globalization;
using RecaptchaExceptions = Hirameku.Recaptcha.Properties.Exceptions;

public static class IRecaptchaResponseValidatorExtensions
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static async Task ValidateAndThrow(
        this IRecaptchaResponseValidator instance,
        string recaptchaResponse,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(
                LogProperties.Parameters,
                new { instance, recaptchaResponse, action, remoteIP, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var result = await instance.Validate(recaptchaResponse, action, remoteIP, cancellationToken)
            .ConfigureAwait(false);

        if (result != RecaptchaVerificationResult.Verified)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                RecaptchaExceptions.RecaptchaVerificationFailed,
                result);

            throw new RecaptchaVerificationFailedException(message);
        }

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Log();
    }
}
