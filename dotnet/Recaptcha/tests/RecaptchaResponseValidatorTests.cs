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

[TestClass]
public class RecaptchaResponseValidatorTests
{
    private const string Action = nameof(Action);
    private const string RecaptchaResponse = nameof(RecaptchaResponse);
    private const string RemoteIP = nameof(RemoteIP);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaResponseValidator_Constructor()
    {
        var target = new RecaptchaResponseValidator(Mock.Of<IRecaptchaClient>(), Mock.Of<IOptions<RecaptchaOptions>>());

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RecaptchaResponseValidator_Validate()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockClient = new Mock<IRecaptchaClient>();
        var expected = RecaptchaVerificationResult.Verified;
        _ = mockClient.Setup(m => m.VerifyResponse(RecaptchaResponse, Action, RemoteIP, cancellationToken))
            .ReturnsAsync(expected);
        var stubOptions = new Mock<IOptions<RecaptchaOptions>>();
        _ = stubOptions.Setup(m => m.Value)
            .Returns(new RecaptchaOptions() { BypassVerification = false });
        var target = new RecaptchaResponseValidator(mockClient.Object, stubOptions.Object);

        var actual = await target.Validate(RecaptchaResponse, Action, RemoteIP, cancellationToken)
            .ConfigureAwait(false);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RecaptcaResponseValidator_Validate_ValidationIsBypassed()
    {
        var expected = RecaptchaVerificationResult.Verified;
        var stubOptions = new Mock<IOptions<RecaptchaOptions>>();
        _ = stubOptions.Setup(m => m.Value)
            .Returns(new RecaptchaOptions() { BypassVerification = true });
        var target = new RecaptchaResponseValidator(Mock.Of<IRecaptchaClient>(), stubOptions.Object);

        var actual = await target.Validate(RecaptchaResponse, Action, RemoteIP).ConfigureAwait(false);

        Assert.AreEqual(expected, actual);
    }
}
