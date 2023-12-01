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

namespace Hirameku.Data.Tests;

using Bogus;
using FluentValidation.TestHelper;
using Hirameku.Common;
using Constants = Hirameku.TestTools.Constants;

[TestClass]
public class CardDocumentValidatorTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocumentValidator_Constructor()
    {
        var target = new CardDocumentValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocumentValidator_Expression_IsWhiteSpace()
    {
        var target = new CardDocumentValidator();
        var card = GetValidCardDocument();
        card.Expression = " \t\r\n";

        _ = target.TestValidate(card)
            .ShouldHaveValidationErrorFor(c => c.Expression)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(0)]
    [DataRow(Constants.InvalidShortLength)]
    public void CardDocumentValidator_Expression_LengthIsInvalid(int length)
    {
        var target = new CardDocumentValidator();
        var random = new Faker().Random;
        var card = GetValidCardDocument();
        card.Expression = random.Utf16String(length, length);

        _ = target.TestValidate(card)
            .ShouldHaveValidationErrorFor(c => c.Expression)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocumentValidator_Id_LengthIsInvalid()
    {
        var target = new CardDocumentValidator();
        var card = GetValidCardDocument();
        card.Id = "1";

        _ = target.TestValidate(card)
            .ShouldHaveValidationErrorFor(c => c.Id)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocumentValidator_Id_PatternIsInvalid()
    {
        var target = new CardDocumentValidator();
        var card = GetValidCardDocument();
        var random = new Faker().Random;
        card.Id = random.String(Constants.ValidIdLength, 'g', 'z');

        _ = target.TestValidate(card)
            .ShouldHaveValidationErrorFor(c => c.Id)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(0)]
    [DataRow(Constants.InvalidNumberOfMeanings)]
    public void CardDocumentValidator_Meanings_LengthIsInvalid(int length)
    {
        var target = new CardDocumentValidator();
        var card = GetValidCardDocument();
        card.Meanings = GetFakeMeanings(length);

        _ = target.TestValidate(card)
            .ShouldHaveValidationErrorFor(c => c.Meanings)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocumentValidator_Notes_LengthIsInvalid()
    {
        var target = new CardDocumentValidator();
        var card = GetValidCardDocument();
        var random = new Faker().Random;
        card.Notes = random.Utf16String(Constants.InvalidLongLength, Constants.InvalidLongLength);

        _ = target.TestValidate(card)
            .ShouldHaveValidationErrorFor(c => c.Notes)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocumentValidator_Notes_PatternIsInvalid()
    {
        var target = new CardDocumentValidator();
        var card = GetValidCardDocument();
        card.Notes = " \t\r\n";

        _ = target.TestValidate(card)
            .ShouldHaveValidationErrorFor(c => c.Notes)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocumentValidator_Reading_LengthIsInvalid()
    {
        var target = new CardDocumentValidator();
        var card = GetValidCardDocument();
        var random = new Faker().Random;
        card.Reading = random.Utf16String(Constants.InvalidShortLength, Constants.InvalidLongLength);

        _ = target.TestValidate(card)
            .ShouldHaveValidationErrorFor(c => c.Reading)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocumentValidator_Reading_PatternIsInvalid()
    {
        var target = new CardDocumentValidator();
        var card = GetValidCardDocument();
        card.Reading = " \t\r\n";

        _ = target.TestValidate(card)
            .ShouldHaveValidationErrorFor(c => c.Reading)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow("")]
    [DataRow("1234567890abcdef12345678")]
    public void CardDocumentValidator_Validate(string id)
    {
        var target = new CardDocumentValidator();
        var card = GetValidCardDocument();
        card.Id = id;

        target.TestValidate(card).ShouldNotHaveAnyValidationErrors();
    }

    private static CardDocument GetValidCardDocument()
    {
        var faker = new Faker();
        var random = faker.Random;
        var meaningFaker = new Faker<Meaning>()
            .RuleFor(m => m.Example, string.Empty)
            .RuleFor(m => m.Hint, string.Empty)
            .RuleFor(m => m.Text, random.Word());

        return new CardDocument()
        {
            CreationDate = faker.Date.Recent(),
            Expression = random.Word(),
            Id = random.Hexadecimal(Constants.ValidIdLength, string.Empty),
            Meanings = GetFakeMeanings(),
            Notes = string.Empty,
            Reading = string.Empty,
            Tags = Enumerable.Empty<string>(),
        };
    }

    private static List<Meaning> GetFakeMeanings(int length = 1)
    {
        var meaningFaker = new Faker<Meaning>()
            .RuleFor(m => m.Example, string.Empty)
            .RuleFor(m => m.Hint, string.Empty)
            .RuleFor(m => m.Text, new Faker().Random.Word());

        return meaningFaker.GenerateBetween(length, length);
    }
}
