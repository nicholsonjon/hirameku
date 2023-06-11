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

[TestClass]
public class RenewTokenModelValidatorTests
{
    private const string InvalidString = "!@#$%^&*()_+,;";

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RenewTokenModelValidator_Constructor()
    {
        var target = new RenewTokenModelValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RenewTokenModelValidator_ClientId_PatternIsInvalid()
    {
        var target = new RenewTokenModelValidator();
        var model = GetModel();
        model.ClientId = InvalidString;

        var result = target.TestValidate(model);

        _ = result.ShouldHaveValidationErrorFor(m => m.ClientId).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RenewTokenModelValidator_ClientToken_PatternIsInvalid()
    {
        var target = new RenewTokenModelValidator();
        var model = GetModel();
        model.ClientToken = InvalidString;

        var result = target.TestValidate(model);

        _ = result.ShouldHaveValidationErrorFor(m => m.ClientToken).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RenewTokenModelValidator_UserId_LengthIsTooLong()
    {
        var target = new RenewTokenModelValidator();
        var model = GetModel();
        model.UserId = "1234567890abcdef123456789";

        var result = target.TestValidate(model);

        _ = result.ShouldHaveValidationErrorFor(m => m.UserId).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RenewTokenModelValidator_UserId_LengthIsTooShort()
    {
        var target = new RenewTokenModelValidator();
        var model = GetModel();
        model.UserId = "1234567890abcdef1234567";

        var result = target.TestValidate(model);

        _ = result.ShouldHaveValidationErrorFor(m => m.UserId).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RenewTokenModelValidator_UserId_PatternIsInvalid()
    {
        var target = new RenewTokenModelValidator();
        var model = GetModel();
        model.UserId = InvalidString;

        var result = target.TestValidate(model);

        _ = result.ShouldHaveValidationErrorFor(m => m.UserId).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RenewTokenValidator_Validate()
    {
        var target = new RenewTokenModelValidator();

        var result = target.TestValidate(GetModel());

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static RenewTokenModel GetModel()
    {
        return new RenewTokenModel()
        {
            ClientId = @"lU{EFzGFDX",
            ClientToken = "Q2xpZW50VG9rZW4=",
            UserId = "1234567890abcdef12345678",
        };
    }
}
