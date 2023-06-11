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

using Moq;

[TestClass]
public class IRecaptchaResponseValidatorExtensionsTests
{
    private const string Action = nameof(Action);
    private const string Hostname = nameof(Hostname);
    private const string RecaptchaResponse = nameof(RecaptchaResponse);
    private const string RemoteIP = nameof(RemoteIP);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task IRecaptchaResponseValidatorExtensions_ValidateAndThrow()
    {
        await RunValidateAndThrowTest(RecaptchaVerificationResult.Verified).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task IRecaptchaResponseValidatorExtensions_ValidateAndThrow_InstanceIsNull_Throws()
    {
        await IRecaptchaResponseValidatorExtensions.ValidateAndThrow(
            null!,
            RecaptchaResponse,
            Hostname,
            Action,
            RemoteIP)
            .ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(RecaptchaVerificationFailedException))]
    [DataRow(RecaptchaVerificationResult.InsufficientScore)]
    [DataRow(RecaptchaVerificationResult.InvalidAction)]
    [DataRow(RecaptchaVerificationResult.InvalidHost)]
    [DataRow(RecaptchaVerificationResult.NotVerified)]
    public async Task IRecaptchaResponseValidatorExtensions_ValidateAndThrows_NotVerified_Throws(
        RecaptchaVerificationResult result)
    {
        await RunValidateAndThrowTest(result).ConfigureAwait(false);
    }

    private static Task RunValidateAndThrowTest(RecaptchaVerificationResult result)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockInstance = new Mock<IRecaptchaResponseValidator>();
        _ = mockInstance.Setup(m => m.Validate(RecaptchaResponse, Hostname, Action, RemoteIP, cancellationToken))
            .ReturnsAsync(result);

        return IRecaptchaResponseValidatorExtensions.ValidateAndThrow(
            mockInstance.Object,
            RecaptchaResponse,
            Hostname,
            Action,
            RemoteIP,
            cancellationToken);
    }
}
