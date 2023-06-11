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
using Hirameku.TestTools;

[TestClass]
public class SignInModelValidatorTests
{
    private const string Password = nameof(Password);
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SignInModelValidator_Constructor()
    {
        var target = new SignInModelValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(SignInModelValidator_Password) + "(null)")]
    [DataRow("", DisplayName = nameof(SignInModelValidator_Password) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(SignInModelValidator_Password) + "(WhiteSpace)")]
    public async Task SignInModelValidator_Password(string password)
    {
        var target = new SignInModelValidator();
        var model = GetValidModel();
        model.Password = password;

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.Password).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task SignInModelValidator_UserName_LengthIsTooLong()
    {
        var target = new SignInModelValidator();
        var model = GetValidModel();
        model.UserName = TestData.GetRandomUserName(Constants.MaxUserNameLength) + "1";

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.UserName).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task SignInModelValidator_UserName_LengthIsTooShort()
    {
        var target = new SignInModelValidator();
        var model = GetValidModel();
        model.UserName = TestData.GetRandomUserName(0, Constants.MinUserNameLength - 1);

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.UserName).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task SignInModelValidator_UserName_PatternIsInvalid()
    {
        var target = new SignInModelValidator();
        var model = GetValidModel();
        model.UserName = "このストリングはインバリッドです";

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.UserName).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task SignInModelValidator_Validate()
    {
        var model = GetValidModel();
        var target = new SignInModelValidator();

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static SignInModel GetValidModel()
    {
        return new SignInModel()
        {
            Password = Password,
            RememberMe = true,
            UserName = UserName,
        };
    }
}
