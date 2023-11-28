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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using RecaptchaExceptions = Hirameku.Recaptcha.Properties.Exceptions;

public class RecaptchaClient : IRecaptchaClient
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Lazy<JsonSerializerOptions> JsonSerializerOptions = new(
        () => new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private static readonly IEnumerable<string> UnexpectedErrorCodes = new List<string>()
    {
        "bad-request",
        "invalid-input-secret",
        "missing-input-response",
        "missing-input-secret",
    };

    public RecaptchaClient(HttpClient client, IOptions<RecaptchaOptions> options)
    {
        this.Client = client;
        this.Options = options;
    }

    private HttpClient Client { get; }

    private IOptions<RecaptchaOptions> Options { get; }

    public async Task<RecaptchaVerificationResult> VerifyResponse(
        string recaptchaResponse,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { recaptchaResponse, action, remoteIP, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        if (string.IsNullOrWhiteSpace(recaptchaResponse))
        {
            throw new ArgumentException(RecaptchaExceptions.StringNullOrWhiteSpace, nameof(recaptchaResponse));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException(RecaptchaExceptions.StringNullOrWhiteSpace, nameof(action));
        }

        if (string.IsNullOrWhiteSpace(remoteIP))
        {
            throw new ArgumentException(RecaptchaExceptions.StringNullOrWhiteSpace, nameof(remoteIP));
        }

        var response = await this.VerifyResponse(recaptchaResponse, remoteIP, cancellationToken)
            .ConfigureAwait(false);
        var result = this.GetRecaptchaVerificationResult(action, response);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, new { returnValue = result })
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }

    [SuppressMessage("Performance", "CA1851:Possible multiple enumerations of 'IEnumerable' collection", Justification = "Not possible in this context")]
    private RecaptchaVerificationResult GetRecaptchaVerificationResult(string action, RecaptchaResponse response)
    {
        RecaptchaVerificationResult result;
        var errorCodes = response.ErrorCodes ?? Enumerable.Empty<string>();
        var options = this.Options.Value;

        if (errorCodes.Any())
        {
            var unexpectedErrorCodes = errorCodes.Intersect(UnexpectedErrorCodes);

            if (unexpectedErrorCodes.Any())
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    RecaptchaExceptions.UnexpectedRecaptchaError,
                    string.Join(", ", UnexpectedErrorCodes));

                throw new InvalidOperationException(message);
            }
            else
            {
                result = RecaptchaVerificationResult.NotVerified;
            }
        }
        else if (!string.Equals(response.Hostname, options.ExpectedHostname, StringComparison.OrdinalIgnoreCase))
        {
            result = RecaptchaVerificationResult.InvalidHost;
        }
        else if (!string.Equals(action, response.Action, StringComparison.OrdinalIgnoreCase))
        {
            result = RecaptchaVerificationResult.InvalidAction;
        }
        else
        {
            result = response.Success && options.MinimumScore <= response.Score
                ? RecaptchaVerificationResult.Verified
                : RecaptchaVerificationResult.NotVerified;
        }

        return result;
    }

    private async Task<RecaptchaResponse> VerifyResponse(
        string responseToken,
        string remoteIP,
        CancellationToken cancellationToken)
    {
        var options = this.Options.Value;
        var siteSecret = options.SiteSecret;

        Log.ForDebugEvent()
            .Property(LogProperties.Data, new { remoteIP, responseToken, siteSecret })
            .Message("reCAPTCHA API request")
            .Log();

        var parameters = new Dictionary<string, string>()
        {
            { "remoteip", remoteIP },
            { "response", responseToken },
            { "secret", siteSecret },
        };
        using var requestContent = new FormUrlEncodedContent(parameters);
        using var response = await this.Client.PostAsync(options.VerificationUrl, requestContent, cancellationToken)
            .ConfigureAwait(false);
        var responseContent = response?.Content;
        var recaptchaResponse = responseContent != null
            ? await responseContent.ReadFromJsonAsync<RecaptchaResponse>(
                JsonSerializerOptions.Value,
                cancellationToken)
                .ConfigureAwait(false)
            : null;

        Log.ForDebugEvent()
            .Property(LogProperties.Data, recaptchaResponse)
            .Message("reCAPTCHA API response")
            .Log();

        return recaptchaResponse ?? new RecaptchaResponse();
    }
}
