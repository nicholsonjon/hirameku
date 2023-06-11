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

namespace Hirameku.User.Tests;

using FluentValidation.TestHelper;

[TestClass]
public class UpdateUserNameModelValidatorTests
{
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UpdateUserNameModelValidator_Constructor()
    {
        var target = new UpdateUserNameModelValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UpdateUserNameModelValidator_UserName_PatternIsInvalid()
    {
        var target = new UpdateUserNameModelValidator();

        var result = await target.TestValidateAsync(
            new UpdateUserNameModel() { UserName = @"`~!@#$%^&*()-_=+|\}]{[';:/?.>,<" })
            .ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.UserName).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UpdateUserNameModelValidator_Validate()
    {
        var target = new UpdateUserNameModelValidator();

        var result = await target.TestValidateAsync(new UpdateUserNameModel() { UserName = UserName })
            .ConfigureAwait(false);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
