﻿// Hirameku is a cloud-native, vendor-agnostic, serverless application for
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
public class ReviewValidatorTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewValidator_Constructor()
    {
        var target = new ReviewValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewValidator_CardId_LengthIsInvalid()
    {
        var target = new ReviewValidator();
        var review = GetValidReview();
        var random = new Faker().Random;
        review.CardId = random.Hexadecimal(Constants.InvalidIdLength, string.Empty);

        _ = target.TestValidate(review)
            .ShouldHaveValidationErrorFor(r => r.CardId)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewValidator_CardId_PatternIsInvalid()
    {
        var target = new ReviewValidator();
        var review = GetValidReview();
        var random = new Faker().Random;
        review.CardId = random.String(Constants.ValidIdLength, 'g', 'z');

        _ = target.TestValidate(review)
            .ShouldHaveValidationErrorFor(r => r.CardId)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewValidator_Disposition_IsInvalid()
    {
        var target = new ReviewValidator();
        var review = GetValidReview();
        review.Disposition = (Disposition)(-1);

        _ = target.TestValidate(review)
            .ShouldHaveValidationErrorFor(r => r.Disposition)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewValidator_Interval_IsInvalid()
    {
        var target = new ReviewValidator();
        var review = GetValidReview();
        var random = new Faker().Random;
        review.Interval = (Interval)(-1);

        _ = target.TestValidate(review)
            .ShouldHaveValidationErrorFor(r => r.Interval)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewValidator_UserId_LengthIsInvalid()
    {
        var target = new ReviewValidator();
        var review = GetValidReview();
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
        var target = new ReviewValidator();
        var review = GetValidReview();
        var random = new Faker().Random;
        review.UserId = random.String(Constants.ValidIdLength, 'g', 'z');

        _ = target.TestValidate(review)
            .ShouldHaveValidationErrorFor(r => r.UserId)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewValidator_Validate()
    {
        var target = new ReviewValidator();
        var review = GetValidReview();

        target.TestValidate(review).ShouldNotHaveAnyValidationErrors();
    }

    private static Review GetValidReview()
    {
        var faker = new Faker();
        var random = faker.Random;

        return new Review()
        {
            CardId = random.Hexadecimal(Constants.ValidIdLength, string.Empty),
            Disposition = random.Enum<Disposition>(),
            Interval = random.Enum<Interval>(),
            ReviewDate = faker.Date.Recent(),
            UserId = random.Hexadecimal(Constants.ValidIdLength, string.Empty),
        };
    }
}
