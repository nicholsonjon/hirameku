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
public class UserValidatorTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserValidator_Constructor()
    {
        var target = new UserValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserValidator_EmailAddress_LengthIsInvalid()
    {
        var target = new UserValidator();
        var user = GetValidUser();
        var random = new Faker().Random;
        user.EmailAddress = random.AlphaNumeric(Constants.InvalidShortLength);

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.EmailAddress)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserValidator_EmailAddress_PatternIsInvalid()
    {
        var target = new UserValidator();
        var user = GetValidUser();
        var random = new Faker().Random;
        user.EmailAddress = random.Words();

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.EmailAddress)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserValidator_Name_IsEmpty()
    {
        var target = new UserValidator();
        var user = GetValidUser();
        user.Name = string.Empty;

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.Name)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserValidator_Name_LengthIsInvalid()
    {
        var target = new UserValidator();
        var user = GetValidUser();
        var random = new Faker().Random;
        user.Name = random.Utf16String(Constants.InvalidShortLength, Constants.InvalidLongLength);

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.Name)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserValidator_Name_IsWhiteSpace()
    {
        var target = new UserValidator();
        var user = GetValidUser();
        user.Name = " \r\t\n";

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.Name)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserValidator_UserName_LengthIsTooLong()
    {
        var target = new UserValidator();
        var user = GetValidUser();
        user.UserName = TestData.GetRandomUserName(Constants.MaxUserNameLength) + "1";

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.UserName)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserValidator_UserName_LengthIsTooShort()
    {
        var target = new UserValidator();
        var user = GetValidUser();
        user.UserName = TestData.GetRandomUserName(0, Constants.MinUserNameLength - 1);

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.UserName)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserValidator_UserName_PatternIsInvalid()
    {
        var target = new UserValidator();
        var user = GetValidUser();
        user.UserName = "このストリングはインバリッドです";

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.UserName)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserValidator_UserStatus_IsInvalid()
    {
        var target = new UserValidator();
        var user = GetValidUser();
        user.UserStatus = (UserStatus)(-1);

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.UserStatus)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserValidator_Validate()
    {
        var target = new UserValidator();
        var user = GetValidUser();

        target.TestValidate(user).ShouldNotHaveAnyValidationErrors();
    }

    private static User GetValidUser()
    {
        var random = new Faker().Random;

        return new User()
        {
            EmailAddress = "test@test.local",
            Name = random.Word(),
            UserName = TestData.GetRandomUserName(),
            UserStatus = UserStatus.OK,
        };
    }
}
