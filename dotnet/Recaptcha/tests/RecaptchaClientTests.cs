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

namespace Hirameku.Recaptcha.Tests;

using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;

[TestClass]
public class RecaptchaClientTests
{
    private const string Action = nameof(Action);
    private const string Hostname = nameof(Hostname);
    private const double MinimumScore = 0.5d;
    private const string RecaptchaResponse = nameof(RecaptchaResponse);
    private const string RemoteIP = nameof(RemoteIP);
    private const string SiteSecret = nameof(SiteSecret);
    private static readonly Lazy<JsonSerializerOptions> JsonSerializerOptions = new(
        () => new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private static readonly Uri VerificationUrl = new("http://localhost");

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaClient_Constructor()
    {
        var target = new RecaptchaClient(
            new Mock<HttpClient>().Object,
            new Mock<IOptions<RecaptchaOptions>>().Object);

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RecaptchaClient_VerifyResponse()
    {
        await RunAndAssertVerifyReponse(RecaptchaVerificationResult.Verified).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow("invalid-input-response")]
    [DataRow("timeout-or-duplicate")]
    public async Task RecaptchaClient_VerifyResponse_ExpectedErrorCode(string errorCode)
    {
        var recaptchaResponse = GetRecaptchaResponse();
        recaptchaResponse.ErrorCodes = new List<string>() { errorCode };

        await RunAndAssertVerifyReponse(RecaptchaVerificationResult.NotVerified, recaptchaResponse)
            .ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RecaptchaClient_VerifyResponse_InvalidAction()
    {
        var recaptchaResponse = GetRecaptchaResponse();
        recaptchaResponse.Action = string.Empty;

        await RunAndAssertVerifyReponse(RecaptchaVerificationResult.InvalidAction, recaptchaResponse)
            .ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RecaptchaClient_VerifyResponse_InvalidHost()
    {
        var recaptchaResponse = GetRecaptchaResponse();
        recaptchaResponse.Hostname = string.Empty;

        await RunAndAssertVerifyReponse(RecaptchaVerificationResult.InvalidHost, recaptchaResponse)
            .ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidOperationException))]
    [DataRow("bad-request")]
    [DataRow("invalid-input-secret")]
    [DataRow("missing-input-response")]
    [DataRow("missing-input-secret")]
    public async Task RecaptchaClient_VerifyResponse_UnexpectedErrorCode_Throws(string errorCode)
    {
        var recaptchaResponse = GetRecaptchaResponse();
        recaptchaResponse.ErrorCodes = new List<string>() { errorCode };

        await RunAndAssertVerifyReponse(RecaptchaVerificationResult.NotVerified, recaptchaResponse)
            .ConfigureAwait(false);

        Assert.Fail(nameof(InvalidOperationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(RecaptchaClient_VerifyResponse_Action_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(RecaptchaClient_VerifyResponse_Action_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(RecaptchaClient_VerifyResponse_Action_Throws) + "(WhiteSpace)")]
    public async Task RecaptchaClient_VerifyResponse_Action_Throws(string action)
    {
        var target = GetTarget();

        _ = await target.VerifyResponse(RecaptchaResponse, Hostname, action, RemoteIP, default).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(RecaptchaClient_VerifyResponse_Hostname_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(RecaptchaClient_VerifyResponse_Hostname_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(RecaptchaClient_VerifyResponse_Hostname_Throws) + "(WhiteSpace)")]
    public async Task RecaptchaClient_VerifyResponse_Hostname_Throws(string hostname)
    {
        var target = GetTarget();

        _ = await target.VerifyResponse(RecaptchaResponse, hostname, Action, RemoteIP, default).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(RecaptchaClient_VerifyResponse_RecaptchaResponse_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(RecaptchaClient_VerifyResponse_RecaptchaResponse_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(RecaptchaClient_VerifyResponse_RecaptchaResponse_Throws) + "(WhiteSpace)")]
    public async Task RecaptchaClient_VerifyResponse_RecaptchaResponse_Throws(string recaptchaResponse)
    {
        var target = GetTarget();

        _ = await target.VerifyResponse(recaptchaResponse, Hostname, Action, RemoteIP, default).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow(null, DisplayName = nameof(RecaptchaClient_VerifyResponse_RemoteIP_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(RecaptchaClient_VerifyResponse_RemoteIP_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(RecaptchaClient_VerifyResponse_RemoteIP_Throws) + "(WhiteSpace)")]
    public async Task RecaptchaClient_VerifyResponse_RemoteIP_Throws(string remoteIP)
    {
        var target = GetTarget();

        _ = await target.VerifyResponse(RecaptchaResponse, Hostname, Action, remoteIP, default).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive")]
    private static async Task AssertRequest(HttpRequestMessage message)
    {
        // this is a Code Analysis false positive (apparently, it doesn't understand the null coalesce operator)
        using var content = message.Content ?? JsonContent.Create("{}");
        var expectedRequest = GetRequest();
        var actualRequest = await content.ReadFromJsonAsync<RecaptchaRequest>().ConfigureAwait(false);

        Assert.AreEqual(expectedRequest.RemoteIP, actualRequest?.RemoteIP);
        Assert.AreEqual(expectedRequest.Response, actualRequest?.Response);
        Assert.AreEqual(expectedRequest.Secret, actualRequest?.Secret);
    }

    private static RecaptchaResponse GetRecaptchaResponse()
    {
        return new RecaptchaResponse()
        {
            Action = Action,
            ChallengeTimestamp = DateTime.UtcNow,
            ErrorCodes = Enumerable.Empty<string>(),
            Hostname = Hostname,
            Score = MinimumScore,
            Success = true,
        };
    }

    private static RecaptchaRequest GetRequest()
    {
        return new RecaptchaRequest()
        {
            RemoteIP = RemoteIP,
            Response = RecaptchaResponse,
            Secret = SiteSecret,
        };
    }

    private static JsonContent GetResponseContent(RecaptchaResponse? recaptchaResponse = default)
    {
        return JsonContent.Create(
            recaptchaResponse ?? GetRecaptchaResponse(),
            new MediaTypeHeaderValue(MediaTypeNames.Application.Json),
            JsonSerializerOptions.Value);
    }

    private static RecaptchaClient GetTarget(
        Mock<HttpClient>? mockClient = default,
        Mock<IOptions<RecaptchaOptions>>? mockOptions = default)
    {
        return new RecaptchaClient(
            (mockClient ?? new Mock<HttpClient>()).Object,
            (mockOptions ?? new Mock<IOptions<RecaptchaOptions>>()).Object);
    }

    private static async Task RunAndAssertVerifyReponse(
        RecaptchaVerificationResult expectedResult,
        RecaptchaResponse? recaptchaResponse = default)
    {
        using var responseMessage = new HttpResponseMessage() { Content = GetResponseContent(recaptchaResponse) };
        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(
                async (m, _) => await AssertRequest(m).ConfigureAwait(false))
            .ReturnsAsync(responseMessage);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockOptions = new Mock<IOptions<RecaptchaOptions>>();
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new RecaptchaOptions()
            {
                MinimumScore = MinimumScore,
                SiteSecret = SiteSecret,
                VerificationUrl = VerificationUrl,
            });
        using var client = new HttpClient(mockHandler.Object);
        var target = new RecaptchaClient(client, mockOptions.Object);

        var actualResult = await target.VerifyResponse(RecaptchaResponse, Hostname, Action, RemoteIP, cancellationToken)
            .ConfigureAwait(false);

        Assert.AreEqual(expectedResult, actualResult);
    }
}
