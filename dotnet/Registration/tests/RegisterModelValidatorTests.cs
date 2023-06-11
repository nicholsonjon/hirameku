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
using Hirameku.Common.Service;
using Hirameku.Registration;
using Hirameku.TestTools;
using Moq;
using CommonPasswordValidationResult = Hirameku.Common.Service.PasswordValidationResult;
using Constants = Hirameku.TestTools.Constants;

[TestClass]
public class RegisterModelValidatorTests
{
    private const string EmailAddress = "test@test.local";
    private const string Password = TestData.Password;

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegisterModelValidator_Constructor()
    {
        var target = new RegisterModelValidator(new Mock<IPasswordValidator>().Object);

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegisterModelValidator_EmailAddress_LengthIsInvalid()
    {
        var target = GetTarget();
        var model = GetModel();
        var random = new Faker().Random;
        model.EmailAddress = random.AlphaNumeric(Constants.InvalidShortLength) + "@localhost";

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.EmailAddress).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegisterModelValidator_EmailAddress_PatternIsInvalid()
    {
        var target = GetTarget();
        var model = GetModel();
        var random = new Faker().Random;
        model.EmailAddress = random.Words();

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.EmailAddress).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(RegisterModelValidator_Name) + "(null)")]
    [DataRow("", DisplayName = nameof(RegisterModelValidator_Name) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(RegisterModelValidator_Name) + "(WhiteSpace)")]
    public async Task RegisterModelValidator_Name(string name)
    {
        var target = GetTarget();
        var model = GetModel();
        model.Name = name;

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.Name).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegisterModelValidator_Name_LengthIsInvalid()
    {
        var target = GetTarget();
        var model = GetModel();
        var random = new Faker().Random;
        model.Name = random.Utf16String(Constants.InvalidShortLength, Constants.InvalidLongLength);

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.Name).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(RegisterModelValidator_Password) + "(null)")]
    [DataRow("", DisplayName = nameof(RegisterModelValidator_Password) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(RegisterModelValidator_Password) + "(WhiteSpace)")]
    public async Task RegisterModelValidator_Password(string password)
    {
        var target = GetTarget();
        var model = GetModel();
        model.Password = password;

        var result = await target.TestValidateAsync(model)
            .ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.Password).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(CommonPasswordValidationResult.Blacklisted)]
    [DataRow(CommonPasswordValidationResult.InsufficientEntropy)]
    [DataRow(CommonPasswordValidationResult.TooLong)]
    public async Task RegisterModelValidator_Password_IsInvalid(CommonPasswordValidationResult result)
    {
        var mockPasswordValidator = GetMockPasswordValidator(result);
        var target = GetTarget(mockPasswordValidator);
        var model = GetModel();

        var validationResult = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = validationResult.ShouldHaveValidationErrorFor(m => m.Password).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task RegisterModelValidator_Password_IsInvalid_Throws()
    {
        var mockPasswordValidator = GetMockPasswordValidator((CommonPasswordValidationResult)(-1));
        var target = GetTarget(mockPasswordValidator);
        var model = GetModel();

        _ = await target.ValidateAsync(model).ConfigureAwait(false);

        Assert.Fail(nameof(InvalidOperationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(RegisterModelValidator_RecaptchaResponse) + "(null)")]
    [DataRow("", DisplayName = nameof(RegisterModelValidator_RecaptchaResponse) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(RegisterModelValidator_RecaptchaResponse) + "(WhiteSpace)")]
    public async Task RegisterModelValidator_RecaptchaResponse(string recaptchaResponse)
    {
        var target = GetTarget();
        var model = GetModel();
        model.RecaptchaResponse = recaptchaResponse;

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.RecaptchaResponse).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegisterModelValidator_RecaptchaResponse_LengthIsTooLong()
    {
        var target = GetTarget();
        var model = GetModel();
        var random = new Faker().Random;
        model.RecaptchaResponse = random.Utf16String(Constants.InvalidShortLength, Constants.InvalidLongLength);

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.RecaptchaResponse).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegisterModelValidator_UserName_LengthIsTooLong()
    {
        var target = GetTarget();
        var model = GetModel();
        model.UserName = TestData.GetRandomUserName(Constants.MaxUserNameLength) + "1";

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.UserName).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegisterModelValidator_UserName_LengthIsTooShort()
    {
        var target = GetTarget();
        var model = GetModel();
        model.UserName = TestData.GetRandomUserName(0, Constants.MinUserNameLength - 1);

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.UserName).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegisterModelValidator_UserName_PatternIsInvalid()
    {
        var target = GetTarget();
        var model = GetModel();
        model.UserName = "このストリングはインバリッドです";

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.UserName).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task RegisterModelValidator_Validate()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockValidator = GetMockPasswordValidator(cancellationToken: cancellationToken);
        var target = GetTarget(mockValidator);

        var result = await target.TestValidateAsync(GetModel(), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static Mock<IPasswordValidator> GetMockPasswordValidator(
        CommonPasswordValidationResult result = CommonPasswordValidationResult.Valid,
        CancellationToken cancellationToken = default)
    {
        var mockValidator = new Mock<IPasswordValidator>();
        _ = mockValidator.Setup(m => m.Validate(TestData.Password, cancellationToken))
            .ReturnsAsync(result);

        return mockValidator;
    }

    private static RegisterModel GetModel()
    {
        return new RegisterModel()
        {
            EmailAddress = EmailAddress,
            Name = nameof(RegisterModel.Name),
            Password = Password,
            RecaptchaResponse = nameof(RegisterModel.RecaptchaResponse),
            UserName = nameof(RegisterModel.UserName),
        };
    }

    private static RegisterModelValidator GetTarget(Mock<IPasswordValidator>? mockPasswordValidator = default)
    {
        return new RegisterModelValidator((mockPasswordValidator ?? GetMockPasswordValidator()).Object);
    }
}
