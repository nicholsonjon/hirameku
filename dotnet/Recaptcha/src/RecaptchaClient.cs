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
using Microsoft.Extensions.Options;
using NLog;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;

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
        string hostname,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new { recaptchaResponse, hostname, action, remoteIP },
            });

        if (string.IsNullOrWhiteSpace(recaptchaResponse))
        {
            throw new ArgumentException(Exceptions.StringNullOrWhiteSpace, nameof(recaptchaResponse));
        }

        if (string.IsNullOrWhiteSpace(hostname))
        {
            throw new ArgumentException(Exceptions.StringNullOrWhiteSpace, nameof(hostname));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException(Exceptions.StringNullOrWhiteSpace, nameof(action));
        }

        if (string.IsNullOrWhiteSpace(remoteIP))
        {
            throw new ArgumentException(Exceptions.StringNullOrWhiteSpace, nameof(remoteIP));
        }

        var response = await this.VerifyResponse(recaptchaResponse, remoteIP, cancellationToken)
            .ConfigureAwait(false);
        var result = this.GetRecaptchaVerificationResult(hostname, action, response);

        Log.Trace("Exiting method", data: new { returnValue = result });

        return result;
    }

    [SuppressMessage("Performance", "CA1851:Possible multiple enumerations of 'IEnumerable' collection", Justification = "Not possible in this context")]
    private RecaptchaVerificationResult GetRecaptchaVerificationResult(
        string hostname,
        string action,
        RecaptchaResponse response)
    {
        RecaptchaVerificationResult result;
        var errorCodes = response.ErrorCodes ?? Enumerable.Empty<string>();

        if (!string.Equals(hostname, response.Hostname, StringComparison.OrdinalIgnoreCase))
        {
            result = RecaptchaVerificationResult.InvalidHost;
        }
        else if (!string.Equals(action, response.Action, StringComparison.OrdinalIgnoreCase))
        {
            result = RecaptchaVerificationResult.InvalidAction;
        }
        else if (errorCodes.Any())
        {
            var unexpectedErrorCodes = errorCodes.Intersect(UnexpectedErrorCodes);

            if (unexpectedErrorCodes.Any())
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    Exceptions.UnexpectedRecaptchaError,
                    string.Join(", ", UnexpectedErrorCodes));

                throw new InvalidOperationException(message);
            }
            else
            {
                result = RecaptchaVerificationResult.NotVerified;
            }
        }
        else
        {
            var options = this.Options.Value;

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
        var recaptchaRequest = new RecaptchaRequest()
        {
            RemoteIP = remoteIP,
            Response = responseToken,
            Secret = options.SiteSecret,
        };

        Log.Debug("reCAPTCHA API request", data: new { recaptchaRequest });

        var mediaType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
        using var requestContent = JsonContent.Create(recaptchaRequest, mediaType, JsonSerializerOptions.Value);
        using var response = await this.Client.PostAsync(options.VerificationUrl, requestContent, cancellationToken)
            .ConfigureAwait(false);
        var responseContent = response?.Content;
        var recaptchaResponse = responseContent != null
            ? await responseContent.ReadFromJsonAsync<RecaptchaResponse>(
                JsonSerializerOptions.Value,
                cancellationToken)
                .ConfigureAwait(false)
            : null;

        Log.Debug("reCAPTCHA API response", data: new { recaptchaResponse });

        return recaptchaResponse ?? new RecaptchaResponse();
    }
}
