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
using Constants = Hirameku.TestTools.Constants;

[TestClass]
public class DeckDocumentValidatorTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DeckDocumentValidator_Constructor()
    {
        var target = new DeckDocumentValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DeckValidator_Cards_InvalidLength()
    {
        var target = new DeckDocumentValidator();
        var deck = GetValidDeckDocument();
        var random = new Faker().Random;
        deck.Cards = random.WordsArray(Constants.InvalidNumberOfCards, Constants.InvalidNumberOfCards);

        _ = target.TestValidate(deck)
            .ShouldHaveValidationErrorFor(d => d.Cards)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DeckValidator_Cards_PatternIsInvalid()
    {
        var target = new DeckDocumentValidator();
        var deck = GetValidDeckDocument();
        var random = new Faker().Random;
        deck.Cards = new List<string>() { "!@#$" };

        _ = target.TestValidate(deck)
            .ShouldHaveValidationErrorFor(d => d.Cards)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DeckDocumentValidator_Id_LengthIsInvalid()
    {
        var target = new DeckDocumentValidator();
        var deck = GetValidDeckDocument();
        var random = new Faker().Random;
        deck.Id = random.Utf16String(Constants.InvalidIdLength);

        _ = target.TestValidate(deck)
            .ShouldHaveValidationErrorFor(d => d.Id)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DeckDocumentValidator_Id_PatternIsInvalid()
    {
        var target = new DeckDocumentValidator();
        var deck = GetValidDeckDocument();
        var random = new Faker().Random;
        deck.Id = random.String(Constants.ValidIdLength, 'g', 'z');

        _ = target.TestValidate(deck)
            .ShouldHaveValidationErrorFor(d => d.Id)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(DeckDocumentValidator_Name_IsNullEmptyOrWhiteSpace) + "(null)")]
    [DataRow("", DisplayName = nameof(DeckDocumentValidator_Name_IsNullEmptyOrWhiteSpace) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(DeckDocumentValidator_Name_IsNullEmptyOrWhiteSpace) + "(WhiteSpace)")]
    public void DeckDocumentValidator_Name_IsNullEmptyOrWhiteSpace(string name)
    {
        var target = new DeckDocumentValidator();
        var deck = GetValidDeckDocument();
        deck.Name = name;

        _ = target.TestValidate(deck)
            .ShouldHaveValidationErrorFor(d => d.Name)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DeckDocumentValidator_Name_LengthIsInvalid()
    {
        var target = new DeckDocumentValidator();
        var deck = GetValidDeckDocument();
        var random = new Faker().Random;
        deck.Name = random.Utf16String(Constants.InvalidShortLength, Constants.InvalidShortLength);

        _ = target.TestValidate(deck)
            .ShouldHaveValidationErrorFor(d => d.Name)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewValidator_UserId_LengthIsInvalid()
    {
        var target = new DeckDocumentValidator();
        var review = GetValidDeckDocument();
        var random = new Faker().Random;
        review.UserId = random.Hexadecimal(Constants.InvalidIdLength, string.Empty);

        _ = target.TestValidate(review)
            .ShouldHaveValidationErrorFor(r => r.UserId)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewValidator_UserId_PatternIsInvalid()
    {
        var target = new DeckDocumentValidator();
        var review = GetValidDeckDocument();
        var random = new Faker().Random;
        review.UserId = random.String(Constants.ValidIdLength, 'g', 'z');

        _ = target.TestValidate(review)
            .ShouldHaveValidationErrorFor(r => r.UserId)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow("")]
    [DataRow("1234567890abcdef12345678")]
    public void DeckDocumentValidator_Validate(string id)
    {
        var target = new DeckDocumentValidator();
        var deck = GetValidDeckDocument();
        deck.Id = id;

        target.TestValidate(deck).ShouldNotHaveAnyValidationErrors();
    }

    private static DeckDocument GetValidDeckDocument()
    {
        var faker = new Faker();
        var random = faker.Random;

        return new DeckDocument()
        {
            Cards = new List<string>() { random.Hexadecimal(Constants.ValidIdLength, string.Empty) },
            CreationDate = faker.Date.Recent(),
            Id = random.Hexadecimal(Constants.ValidIdLength, string.Empty),
            Name = random.Word(),
            UserId = random.Hexadecimal(Constants.ValidIdLength, string.Empty),
        };
    }
}
