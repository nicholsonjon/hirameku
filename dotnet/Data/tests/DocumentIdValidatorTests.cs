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
using Hirameku.TestTools;

[TestClass]
public class DocumentIdValidatorTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DocumentIdValidator_Validate()
    {
        var target = new DocumentIdValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DocumentIdValidator_Id_LengthIsInvalid()
    {
        var target = new DocumentIdValidator();
        var random = new Faker().Random;
        var document = new TestDocument(random.Utf16String(Constants.InvalidIdLength));

        _ = target.TestValidate(document)
            .ShouldHaveValidationErrorFor(d => d.Id)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DocumentIdValidator_Id_PatternIsInvalid()
    {
        var target = new DocumentIdValidator();
        var random = new Faker().Random;
        var document = new TestDocument(random.String(Constants.ValidIdLength, 'g', 'z'));

        _ = target.TestValidate(document)
            .ShouldHaveValidationErrorFor(d => d.Id)
            .Only();
    }
}
