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

namespace Hirameku.Registration.Tests;

using Bogus;
using FluentValidation.TestHelper;
using Hirameku.TestTools;

[TestClass]
public class ResendVerificationEmailModelValidatorTests
{
    private const string EmailAddress = "test@test.local";
    private const string RecaptchaResponse = nameof(RecaptchaResponse);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResendVerificationEmailModelValidator_Constructor()
    {
        var target = new ResendVerificationEmailModelValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResendVerificationEmailModelValidator_EmailAddress_LengthIsInvalid()
    {
        var target = new ResendVerificationEmailModelValidator();
        var model = GetValidModel();
        var random = new Faker().Random;
        model.EmailAddress = random.AlphaNumeric(Constants.InvalidShortLength) + "@localhost";

        var result = target.TestValidate(model);

        _ = result.ShouldHaveValidationErrorFor(m => m.EmailAddress).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResendVerificationEmailModelValidator_EmailAddress_PatternIsInvalid()
    {
        var target = new ResendVerificationEmailModelValidator();
        var model = GetValidModel();
        var random = new Faker().Random;
        model.EmailAddress = random.Words();

        var result = target.TestValidate(model);

        _ = result.ShouldHaveValidationErrorFor(m => m.EmailAddress).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(ResendVerificationEmailModelValidator_RecaptchaResponse) + "(null)")]
    [DataRow("", DisplayName = nameof(ResendVerificationEmailModelValidator_RecaptchaResponse) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(ResendVerificationEmailModelValidator_RecaptchaResponse) + "(WhiteSpace)")]
    public void ResendVerificationEmailModelValidator_RecaptchaResponse(string recaptchaResponse)
    {
        var target = new ResendVerificationEmailModelValidator();
        var model = GetValidModel();
        model.RecaptchaResponse = recaptchaResponse;

        var result = target.TestValidate(model);

        _ = result.ShouldHaveValidationErrorFor(m => m.RecaptchaResponse).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task ResendVerificationEmailModelValidator_RecaptchaResponse_LengthIsTooLong()
    {
        var target = new ResendVerificationEmailModelValidator();
        var model = GetValidModel();
        var random = new Faker().Random;
        const int Length = Constants.InvalidLongLength;
        model.RecaptchaResponse = random.String(Length, Length);

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.RecaptchaResponse).Only();
    }

    private static ResendVerificationEmailModel GetValidModel()
    {
        return new ResendVerificationEmailModel()
        {
            EmailAddress = EmailAddress,
            RecaptchaResponse = RecaptchaResponse,
        };
    }
}
