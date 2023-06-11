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
using Hirameku.TestTools;
using Constants = Hirameku.TestTools.Constants;

[TestClass]
public class UserDocumentValidatorTests
{
    private const int MaxUserNameLength = 32;
    private const int MinUserNameLength = 4;
    private const int ObjectIdStringLength = 24;

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserDocumentValidator_Constructor()
    {
        var target = new UserDocumentValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserDocumentValidator_EmailAddress_LengthIsInvalid()
    {
        var target = new UserDocumentValidator();
        var user = GetValidUserDocument();
        var random = new Faker().Random;
        user.EmailAddress = random.AlphaNumeric(Constants.InvalidShortLength);

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.EmailAddress)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserDocumentValidator_EmailAddress_PatternIsInvalid()
    {
        var target = new UserDocumentValidator();
        var user = GetValidUserDocument();
        var random = new Faker().Random;
        user.EmailAddress = random.Words();

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.EmailAddress)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserDocumentValidator_Id_LengthIsInvalid()
    {
        var target = new UserDocumentValidator();
        var user = GetValidUserDocument();
        var random = new Faker().Random;
        user.Id = random.Hexadecimal(Constants.InvalidIdLength, string.Empty);

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.Id)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserDocumentValidator_Id_PatternIsInvalid()
    {
        var target = new UserDocumentValidator();
        var user = GetValidUserDocument();
        var random = new Faker().Random;
        user.Id = random.String(Constants.ValidIdLength, 'g', 'z');

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.Id)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserDocumentValidator_Name_IsEmpty()
    {
        var target = new UserDocumentValidator();
        var user = GetValidUserDocument();
        user.Name = string.Empty;

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.Name)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserDocumentValidator_Name_LengthIsInvalid()
    {
        var target = new UserDocumentValidator();
        var user = GetValidUserDocument();
        var random = new Faker().Random;
        user.Name = random.Utf16String(Constants.InvalidShortLength, Constants.InvalidLongLength);

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.Name)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserDocumentValidator_Name_IsWhiteSpace()
    {
        var target = new UserDocumentValidator();
        var user = GetValidUserDocument();
        user.Name = " \r\t\n";

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.Name)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserDocumentValidator_UserName_LengthIsTooLong()
    {
        var target = new UserDocumentValidator();
        var user = GetValidUserDocument();
        user.UserName = TestData.GetRandomUserName(MaxUserNameLength) + "1";

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.UserName)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserDocumentValidator_UserName_LengthIsTooShort()
    {
        var target = new UserDocumentValidator();
        var user = GetValidUserDocument();
        user.UserName = TestData.GetRandomUserName(0, MinUserNameLength - 1);

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.UserName)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserDocumentValidator_UserName_PatternIsInvalid()
    {
        var target = new UserDocumentValidator();
        var user = GetValidUserDocument();
        user.UserName = "このストリングはインバリッドです";

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.UserName)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserDocumentValidator_UserStatus_IsInvalid()
    {
        var target = new UserDocumentValidator();
        var user = GetValidUserDocument();
        user.UserStatus = (UserStatus)(-1);

        _ = target.TestValidate(user)
            .ShouldHaveValidationErrorFor(u => u.UserStatus)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserDocumentValidator_Validate()
    {
        var target = new UserDocumentValidator();
        var user = GetValidUserDocument();

        target.TestValidate(user).ShouldNotHaveAnyValidationErrors();
    }

    private static UserDocument GetValidUserDocument()
    {
        var random = new Faker().Random;

        return new UserDocument()
        {
            EmailAddress = "test@test.local",
            Id = random.Hexadecimal(ObjectIdStringLength, string.Empty),
            Name = random.Word(),
            UserName = TestData.GetRandomUserName(),
            UserStatus = UserStatus.OK,
        };
    }
}
