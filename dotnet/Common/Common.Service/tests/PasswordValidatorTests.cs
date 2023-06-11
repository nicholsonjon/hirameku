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

namespace Hirameku.Common.Service.Tests;

using Bogus;
using Hirameku.TestTools;
using Microsoft.Extensions.Options;
using Moq;
using Nito.AsyncEx;

[TestClass]
public class PasswordValidatorTests
{
    private const int MinPasswordEntropy = 60;
    private const int MaxPasswordLength = 128;

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordValidator_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PasswordValidator_Validate_Blacklisted()
    {
        const string Password = TestData.Password;

        await RunAndAssertValidatePasswordTest(
            Password,
            PasswordValidationResult.Blacklisted,
            blacklist: new List<string>() { Password })
            .ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(3.4d, PasswordValidationResult.InsufficientEntropy)]
    [DataRow(3.3d, PasswordValidationResult.Valid)]
    public async Task PasswordValidator_CalculateEntropy_Digits(
        double minPasswordEntropy,
        PasswordValidationResult expectedResult)
    {
        await RunAndAssertCaculateEntropyTest("0", minPasswordEntropy, expectedResult).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(4.8d, PasswordValidationResult.InsufficientEntropy)]
    [DataRow(4.7d, PasswordValidationResult.Valid)]
    public async Task PasswordValidator_CalculateEntropy_LowerCaseLetters(
        double minPasswordEntropy,
        PasswordValidationResult expectedResult)
    {
        await RunAndAssertCaculateEntropyTest("a", minPasswordEntropy, expectedResult).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(6.6d, PasswordValidationResult.InsufficientEntropy)]
    [DataRow(6.5d, PasswordValidationResult.Valid)]
    public async Task PasswordValidator_CalculateEntropy_NonAsciiCharacters(
        double minPasswordEntropy,
        PasswordValidationResult expectedResult)
    {
        await RunAndAssertCaculateEntropyTest("験", minPasswordEntropy, expectedResult).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(4.0d, PasswordValidationResult.InsufficientEntropy)]
    [DataRow(3.9d, PasswordValidationResult.Valid)]
    public async Task PasswordValidator_CalculateEntropy_Punctuation(
        double minPasswordEntropy,
        PasswordValidationResult expectedResult)
    {
        await RunAndAssertCaculateEntropyTest(" ", minPasswordEntropy, expectedResult).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(4.3d, PasswordValidationResult.InsufficientEntropy)]
    [DataRow(4.2d, PasswordValidationResult.Valid)]
    public async Task PasswordValidator_CalculateEntropy_Symbols(
        double minPasswordEntropy,
        PasswordValidationResult expectedResult)
    {
        await RunAndAssertCaculateEntropyTest("@", minPasswordEntropy, expectedResult).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(4.8d, PasswordValidationResult.InsufficientEntropy)]
    [DataRow(4.7d, PasswordValidationResult.Valid)]
    public async Task PasswordValidator_CalculateEntropy_UpperCaseLetters(
        double minPasswordEntropy,
        PasswordValidationResult expectedResult)
    {
        await RunAndAssertCaculateEntropyTest("A", minPasswordEntropy, expectedResult).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(OperationCanceledException))]
    public async Task PasswordValidator_Validate_CancellationRequested_Throws()
    {
        await RunAndAssertValidatePasswordTest(
            TestData.Password,
            PasswordValidationResult.Valid,
            cancellationToken: new CancellationToken(true))
            .ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(PasswordValidator_Validate_InsufficientEntropy) + "(null)")]
    [DataRow("", DisplayName = nameof(PasswordValidator_Validate_InsufficientEntropy) + "(string.Empty)")]
    [DataRow("a", DisplayName = nameof(PasswordValidator_Validate_InsufficientEntropy) + "(a)")]
    public async Task PasswordValidator_Validate_InsufficientEntropy(string password)
    {
        await RunAndAssertValidatePasswordTest(password, PasswordValidationResult.InsufficientEntropy)
            .ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PasswordValidator_Validate_TooLong()
    {
        var faker = new Faker();
        var password = faker.Random.String2(129);

        await RunAndAssertValidatePasswordTest(password, PasswordValidationResult.TooLong).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PasswordValidator_Validate_Valid()
    {
        await RunAndAssertValidatePasswordTest(TestData.Password, PasswordValidationResult.Valid)
            .ConfigureAwait(false);
    }

    private static PasswordValidator GetTarget(
        IOptions<PasswordValidatorOptions>? options = default)
    {
        return new PasswordValidator(
            options ?? new Mock<IOptions<PasswordValidatorOptions>>().Object,
            new AsyncLazy<IEnumerable<string>>(() => Task.FromResult(new List<string>().AsEnumerable())));
    }

    private static async Task RunAndAssertCaculateEntropyTest(
        string password,
        double minPasswordEntropy,
        PasswordValidationResult expectedResult)
    {
        var mockOptions = new Mock<IOptions<PasswordValidatorOptions>>();
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new PasswordValidatorOptions()
            {
                MaxPasswordLength = MaxPasswordLength,
                MinPasswordEntropy = minPasswordEntropy,
            });
        var target = GetTarget(mockOptions.Object);

        var actualResult = await target.Validate(password).ConfigureAwait(false);

        Assert.AreEqual(expectedResult, actualResult);
    }

    private static async Task RunAndAssertValidatePasswordTest(
        string password,
        PasswordValidationResult expectedResult,
        Mock<IOptions<PasswordValidatorOptions>>? mockOptions = default,
        IEnumerable<string>? blacklist = default,
        CancellationToken cancellationToken = default)
    {
        if (mockOptions == null)
        {
            mockOptions = new Mock<IOptions<PasswordValidatorOptions>>();
            _ = mockOptions.Setup(m => m.Value)
                .Returns(new PasswordValidatorOptions()
                {
                    MaxPasswordLength = MaxPasswordLength,
                    MinPasswordEntropy = MinPasswordEntropy,
                });
        }

        var target = new PasswordValidator(
            mockOptions.Object,
            new AsyncLazy<IEnumerable<string>>(() => Task.FromResult(blacklist ?? new List<string>())));

        var actualResult = await target.Validate(password, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expectedResult, actualResult);
    }
}
