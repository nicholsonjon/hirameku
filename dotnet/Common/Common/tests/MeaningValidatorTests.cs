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

using Bogus;
using FluentValidation.TestHelper;
using Hirameku.TestTools;

[TestClass]
public class MeaningValidatorTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void MeaningValidator_Constructor()
    {
        var target = new MeaningValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void MeaningValidator_Example_LengthIsInvalid()
    {
        var target = new MeaningValidator();
        var meaning = GetValidMeaning();
        var random = new Faker().Random;
        meaning.Example = random.Utf16String(Constants.InvalidLongLength, Constants.InvalidLongLength);

        _ = target.TestValidate(meaning)
            .ShouldHaveValidationErrorFor(m => m.Example)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void MeaningValidator_Example_IsWhiteSpace()
    {
        var target = new MeaningValidator();
        var meaning = GetValidMeaning();
        meaning.Example = " \t\r\n";

        _ = target.TestValidate(meaning)
            .ShouldHaveValidationErrorFor(m => m.Example)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void MeaningValidator_Hint_IsWhiteSpace()
    {
        var target = new MeaningValidator();
        var meaning = GetValidMeaning();
        meaning.Hint = " \t\r\n";

        _ = target.TestValidate(meaning)
            .ShouldHaveValidationErrorFor(m => m.Hint)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void MeaningValidator_Hint_LengthIsInvalid()
    {
        var target = new MeaningValidator();
        var meaning = GetValidMeaning();
        var random = new Faker().Random;
        meaning.Hint = random.Utf16String(Constants.InvalidLongLength, Constants.InvalidLongLength);

        _ = target.TestValidate(meaning)
            .ShouldHaveValidationErrorFor(m => m.Hint)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void MeaningValidator_Text_LengthIsInvalid()
    {
        var target = new MeaningValidator();
        var meaning = GetValidMeaning();
        var random = new Faker().Random;
        meaning.Text = random.Utf16String(Constants.InvalidLongLength, Constants.InvalidLongLength);

        _ = target.TestValidate(meaning)
            .ShouldHaveValidationErrorFor(m => m.Text)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow("", DisplayName = nameof(MeaningValidator_Text) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(MeaningValidator_Text) + "(WhiteSpace)")]
    public void MeaningValidator_Text(string text)
    {
        var target = new MeaningValidator();
        var meaning = GetValidMeaning();
        meaning.Text = text;

        _ = target.TestValidate(meaning)
            .ShouldHaveValidationErrorFor(m => m.Text)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void MeaningValidator_Validate()
    {
        var target = new MeaningValidator();
        var meaning = GetValidMeaning();

        target.TestValidate(meaning).ShouldNotHaveAnyValidationErrors();
    }

    private static Meaning GetValidMeaning()
    {
        return new Meaning()
        {
            Example = string.Empty,
            Hint = string.Empty,
            Text = new Faker().Random.Word(),
        };
    }
}
