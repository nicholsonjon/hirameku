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

using Bogus;
using FluentValidation.TestHelper;
using Hirameku.Common;

[TestClass]
public class UpdateNameModelValidatorTests
{
    private const string Name = nameof(Name);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UpdateNameModelValidator_Constructor()
    {
        var target = new UpdateNameModelValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UpdateNameModelValidator_Name_LengthIsInvalid()
    {
        var target = new UpdateNameModelValidator();
        var faker = new Faker();

        var result = await target.TestValidateAsync(
            new UpdateNameModel() { Name = faker.Random.String2(Constants.MaxStringLengthShort + 1) })
            .ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.Name).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(UpdateNameModelValidator_Name_NullEmptyOrWhiteSpace) + "(null)")]
    [DataRow("", DisplayName = nameof(UpdateNameModelValidator_Name_NullEmptyOrWhiteSpace) + "(string.Empty)")]
    [DataRow("\t\r\n ", DisplayName = nameof(UpdateNameModelValidator_Name_NullEmptyOrWhiteSpace) + "(WhiteSpace)")]
    public async Task UpdateNameModelValidator_Name_NullEmptyOrWhiteSpace(string name)
    {
        var target = new UpdateNameModelValidator();

        var result = await target.TestValidateAsync(new UpdateNameModel() { Name = name }).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.Name).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UpdateNameModelValidator_Validate()
    {
        var target = new UpdateNameModelValidator();

        var result = await target.TestValidateAsync(new UpdateNameModel() { Name = Name }).ConfigureAwait(false);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
