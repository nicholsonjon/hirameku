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

namespace Hirameku.Authentication.Tests;

using FluentValidation.TestHelper;
using Hirameku.Common.Service;
using Hirameku.TestTools;
using Moq;

[TestClass]
public class ResetPasswordModelValidatorTests
{
    private const string Password = nameof(Password);
    private const string RecaptchaResponse = nameof(RecaptchaResponse);
    private const string SerializedToken = TestData.Token;

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResetPasswordModelValidator_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(PasswordValidationResult.Blacklisted)]
    [DataRow(PasswordValidationResult.InsufficientEntropy)]
    [DataRow(PasswordValidationResult.TooLong)]
    public async Task ResetPasswordModelValidator_Password_IsInvalid(PasswordValidationResult passwordResult)
    {
        var mockPasswordValidator = GetMockPasswordValidator(passwordResult);
        var target = GetTarget(mockPasswordValidator);

        var result = await target.TestValidateAsync(GetModel()).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.Password).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(ResetPasswordModelValidator_RecaptchaResponse) + "(null)")]
    [DataRow("", DisplayName = nameof(ResetPasswordModelValidator_RecaptchaResponse) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(ResetPasswordModelValidator_RecaptchaResponse) + "(WhiteSpace)")]
    public async Task ResetPasswordModelValidator_RecaptchaResponse(string recaptchaResponse)
    {
        var target = GetTarget();
        var model = GetModel();
        model.RecaptchaResponse = recaptchaResponse;

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.RecaptchaResponse).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(ResetPasswordModelValidator_SerializedToken_NullEmptyOrWhiteSpace) + "(null)")]
    [DataRow("", DisplayName = nameof(ResetPasswordModelValidator_SerializedToken_NullEmptyOrWhiteSpace) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(ResetPasswordModelValidator_SerializedToken_NullEmptyOrWhiteSpace) + "(WhiteSpace)")]
    public async Task ResetPasswordModelValidator_SerializedToken_NullEmptyOrWhiteSpace(string serializedToken)
    {
        var target = GetTarget();
        var model = GetModel();
        model.SerializedToken = serializedToken;

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.SerializedToken).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task ResetPasswordModelValidator_SerializedToken_PatternIsInvalid()
    {
        var target = GetTarget();
        var model = GetModel();
        model.SerializedToken = "!@#$";

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.SerializedToken).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task ResetPasswordModelValidator_Validate()
    {
        var target = GetTarget();

        var result = await target.TestValidateAsync(GetModel()).ConfigureAwait(false);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static Mock<IPasswordValidator> GetMockPasswordValidator(
        PasswordValidationResult result = PasswordValidationResult.Valid)
    {
        var mockPasswordValidator = new Mock<IPasswordValidator>();
        _ = mockPasswordValidator.Setup(m => m.Validate(Password, default))
            .ReturnsAsync(result);

        return mockPasswordValidator;
    }

    private static ResetPasswordModel GetModel()
    {
        return new ResetPasswordModel()
        {
            Password = Password,
            RecaptchaResponse = RecaptchaResponse,
            SerializedToken = SerializedToken,
        };
    }

    private static ResetPasswordModelValidator GetTarget(Mock<IPasswordValidator>? mockPasswordValidator = default)
    {
        return new ResetPasswordModelValidator((mockPasswordValidator ?? GetMockPasswordValidator()).Object);
    }
}
