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

using Bogus;
using FluentValidation.TestHelper;
using Hirameku.TestTools;

[TestClass]
public class SendPasswordResetModelValidatorTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SendPasswordResetModelValidator_Constructor()
    {
        var target = new SendPasswordResetModelValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(SendPasswordResetModelValidator_RecaptchaResponse_NullEmptyOrWhiteSpace) + "(null)")]
    [DataRow("", DisplayName = nameof(SendPasswordResetModelValidator_RecaptchaResponse_NullEmptyOrWhiteSpace) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(SendPasswordResetModelValidator_RecaptchaResponse_NullEmptyOrWhiteSpace) + "(WhiteSpace)")]
    public async Task SendPasswordResetModelValidator_RecaptchaResponse_NullEmptyOrWhiteSpace(string recaptchaResponse)
    {
        var target = new SendPasswordResetModelValidator();
        var model = GetValidModel();
        model.RecaptchaResponse = recaptchaResponse;

        var result = await target.TestValidateAsync(model)
            .ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.RecaptchaResponse).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task ResetPasswordModelValidator_RecaptchaResponse_TooLong()
    {
        var target = new SendPasswordResetModelValidator();
        var model = GetValidModel();
        var random = new Faker().Random;
        const int Length = Constants.InvalidLongLength;
        model.RecaptchaResponse = random.String(Length, Length);

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.RecaptchaResponse).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(SendPasswordResetModelValidator_UserName) + "(null)")]
    [DataRow("", DisplayName = nameof(SendPasswordResetModelValidator_UserName) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(SendPasswordResetModelValidator_UserName) + "(WhiteSpace)")]
    public async Task SendPasswordResetModelValidator_UserName(string userName)
    {
        var target = new SendPasswordResetModelValidator();
        var model = GetValidModel();
        model.UserName = userName;

        var result = await target.TestValidateAsync(model)
            .ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.UserName).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task ResetPasswordModelValidator_UserName_TooLong()
    {
        var target = new SendPasswordResetModelValidator();
        var model = GetValidModel();
        var random = new Faker().Random;
        const int Length = Constants.InvalidShortLength;
        model.UserName = random.String(Length, Length);

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.UserName).Only();
    }

    private static SendPasswordResetModel GetValidModel()
    {
        return new SendPasswordResetModel()
        {
            RecaptchaResponse = nameof(SendPasswordResetModel.RecaptchaResponse),
            UserName = nameof(SendPasswordResetModel.UserName),
        };
    }
}
