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
public class NameValidatorTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void NameValidator_Constructor()
    {
        var target = new NameValidatorTests();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow("", DisplayName = nameof(NameValidator_EmptyOrWhiteSpace) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(NameValidator_EmptyOrWhiteSpace) + "(WhiteSpace)")]
    public async Task NameValidator_EmptyOrWhiteSpace(string name)
    {
        var target = new NameValidator();

        var result = await target.TestValidateAsync(name).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(s => s).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task NameValidator_Null_Throws()
    {
        var target = new NameValidator();

        _ = await target.TestValidateAsync((null as string)!).ConfigureAwait(false);

        Assert.Fail(typeof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task NameValidator_PatternIsInvalid()
    {
        var target = new NameValidator();
        const string InvalidString = "!@#$%^&*()_+[]\\{}|;:',./<>?";

        var result = await target.TestValidateAsync(InvalidString).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(s => s).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task NameValidator_Validate()
    {
        var target = new NameValidator();
        const string Name = nameof(Name);

        var result = await target.TestValidateAsync(Name).ConfigureAwait(false);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
