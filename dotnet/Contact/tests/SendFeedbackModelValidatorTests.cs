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

namespace Hirameku.Contact.Tests;

using Bogus;
using FluentValidation.TestHelper;
using Hirameku.TestTools;

[TestClass]
public class SendFeedbackModelValidatorTests
{
    private const string EmailAddress = "test@test.local";

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SendFeedbackModelValidator_Constructor()
    {
        var target = new SendFeedbackModelValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task SendFeedbackModelValidator_EmailAddress_LengthIsInvalid()
    {
        var target = new SendFeedbackModelValidator();
        var model = GetModel();
        var random = new Faker().Random;
        model.EmailAddress = random.AlphaNumeric(Constants.InvalidShortLength) + "@localhost";

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.EmailAddress).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task SendFeedbackModelValidator_EmailAddress_PatternIsInvalid()
    {
        var target = new SendFeedbackModelValidator();
        var model = GetModel();
        var random = new Faker().Random;
        model.EmailAddress = random.Words();

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.EmailAddress).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(SendFeedbackModelValidator_Feedback) + "(null)")]
    [DataRow("", DisplayName = nameof(SendFeedbackModelValidator_Feedback) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(SendFeedbackModelValidator_Feedback) + "(WhiteSpace)")]
    public async Task SendFeedbackModelValidator_Feedback(string feedback)
    {
        var target = new SendFeedbackModelValidator();
        var model = GetModel();
        model.Feedback = feedback;

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.Feedback).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task SendFeedbackModelValidator_Feedback_LengthIsInvalid()
    {
        var target = new SendFeedbackModelValidator();
        var model = GetModel();
        var random = new Faker().Random;
        model.Feedback = random.Utf16String(Constants.InvalidLongLength, Constants.InvalidLongLength);

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.Feedback).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(SendFeedbackModelValidator_Name) + "(null)")]
    [DataRow("", DisplayName = nameof(SendFeedbackModelValidator_Name) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(SendFeedbackModelValidator_Name) + "(WhiteSpace)")]
    public async Task SendFeedbackModelValidator_Name(string name)
    {
        var target = new SendFeedbackModelValidator();
        var model = GetModel();
        model.Name = name;

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.Name).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task SendFeedbackModelValidator_Name_LengthIsInvalid()
    {
        var target = new SendFeedbackModelValidator();
        var model = GetModel();
        var random = new Faker().Random;
        model.Name = random.Utf16String(Constants.InvalidShortLength, Constants.InvalidLongLength);

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.Name).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(SendFeedbackModelValidator_RecaptchaResponse) + "(null)")]
    [DataRow("", DisplayName = nameof(SendFeedbackModelValidator_RecaptchaResponse) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(SendFeedbackModelValidator_RecaptchaResponse) + "(WhiteSpace)")]
    public async Task SendFeedbackModelValidator_RecaptchaResponse(string recaptchaResponse)
    {
        var target = new SendFeedbackModelValidator();
        var model = GetModel();
        model.RecaptchaResponse = recaptchaResponse;

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.RecaptchaResponse).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task SendFeedbackModelValidator_RecaptchaResponse_LengthIsTooLong()
    {
        var target = new SendFeedbackModelValidator();
        var model = GetModel();
        var random = new Faker().Random;
        model.RecaptchaResponse = random.Utf16String(Constants.InvalidShortLength, Constants.InvalidLongLength);

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.RecaptchaResponse).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(SendFeedbackModelValidator_RecaptchaResponse) + "(null)")]
    [DataRow(EmailAddress, DisplayName = nameof(SendFeedbackModelValidator_RecaptchaResponse) + "(EmailAddress)")]
    public async Task SendFeedbackModelValidator_Validate(string emailAddress)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var target = new SendFeedbackModelValidator();

        var result = await target.TestValidateAsync(GetModel(emailAddress), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static SendFeedbackModel GetModel(string emailAddress = EmailAddress)
    {
        return new SendFeedbackModel()
        {
            EmailAddress = emailAddress,
            Feedback = nameof(SendFeedbackModel.Feedback),
            Name = nameof(SendFeedbackModel.Name),
            RecaptchaResponse = nameof(SendFeedbackModel.RecaptchaResponse),
        };
    }
}
