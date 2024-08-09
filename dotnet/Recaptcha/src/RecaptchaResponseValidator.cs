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
using Microsoft.Extensions.Options;
using NLog;

public class RecaptchaResponseValidator : IRecaptchaResponseValidator
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public RecaptchaResponseValidator(IRecaptchaClient client, IOptions<RecaptchaOptions> options)
    {
        this.Client = client;
        this.Options = options;
    }

    private IRecaptchaClient Client { get; }

    private IOptions<RecaptchaOptions> Options { get; }

    public async Task<RecaptchaVerificationResult> Validate(
        string recaptchaResponse,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { recaptchaResponse, action, remoteIP, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        RecaptchaVerificationResult result;

        if (!this.Options.Value.BypassValidation)
        {
            result = await this.Client.VerifyResponse(
                recaptchaResponse,
                action,
                remoteIP,
                cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            Log.ForDebugEvent()
                .Message("reCAPTCHA validation is bypassed")
                .Log();

            result = RecaptchaVerificationResult.Verified;
        }

        Log.ForDebugEvent()
            .Property(LogProperties.Data, result)
            .Message("reCAPTCHA verification result")
            .Log();

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, result)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }
}
