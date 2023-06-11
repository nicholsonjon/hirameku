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

namespace Hirameku.Common.Tests;

using FluentValidation.TestHelper;

[TestClass]
public class UserNameValidatorTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserNameValidator_Constructor()
    {
        var target = new UserNameValidatorTests();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow("", DisplayName = nameof(UserNameValidator_EmptyOrWhiteSpace) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(UserNameValidator_EmptyOrWhiteSpace) + "(WhiteSpace)")]
    public async Task UserNameValidator_EmptyOrWhiteSpace(string userName)
    {
        var target = new UserNameValidator();

        var result = await target.TestValidateAsync(userName).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(s => s).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task UserNameValidator_Null_Throws()
    {
        var target = new UserNameValidator();

        _ = await target.TestValidateAsync((null as string)!).ConfigureAwait(false);

        Assert.Fail(typeof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserNameValidator_PatternIsInvalid()
    {
        var target = new UserNameValidator();
        const string InvalidString = "!@#$%^&*()_+[]\\{}|;:',./<>?";

        var result = await target.TestValidateAsync(InvalidString).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(s => s).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserNameValidator_Validate()
    {
        var target = new UserNameValidator();
        const string UserName = nameof(UserName);

        var result = await target.TestValidateAsync(UserName).ConfigureAwait(false);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
