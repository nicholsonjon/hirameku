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

namespace Hirameku.Email.Tests;

using FluentValidation.TestHelper;
using Hirameku.TestTools;

[TestClass]
public class EmailTokenDataValidatorTests
{
    private const string Pepper = TestData.Pepper;
    private const string Token = TestData.Token;
    private const string UserName = nameof(UserName);
    private static readonly TimeSpan ValidityPeriod = TimeSpan.FromDays(1);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailTokenDataValidator_Constructor()
    {
        var target = new EmailTokenDataValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(EmailTokenDataValidator_Pepper_NullEmptyOrWhiteSpace) + "(null)")]
    [DataRow("", DisplayName = nameof(EmailTokenDataValidator_Pepper_NullEmptyOrWhiteSpace) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(EmailTokenDataValidator_Pepper_NullEmptyOrWhiteSpace) + "(WhiteSpace)")]
    public async Task EmailTokenDataValidator_Pepper_NullEmptyOrWhiteSpace(string pepper)
    {
        var target = new EmailTokenDataValidator();
        var data = new EmailTokenData(pepper, Token, UserName, ValidityPeriod);

        var result = await target.TestValidateAsync(data).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(d => d.Pepper).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task EmailTokenDataValidator_Pepper_PatternIsInvalid()
    {
        var target = new EmailTokenDataValidator();
        var data = new EmailTokenData("!@#$", Token, UserName, ValidityPeriod);

        var result = await target.TestValidateAsync(data).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(d => d.Pepper).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(EmailTokenDataValidator_Token_NullEmptyOrWhiteSpace) + "(null)")]
    [DataRow("", DisplayName = nameof(EmailTokenDataValidator_Token_NullEmptyOrWhiteSpace) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(EmailTokenDataValidator_Token_NullEmptyOrWhiteSpace) + "(WhiteSpace)")]
    public async Task EmailTokenDataValidator_Token_NullEmptyOrWhiteSpace(string token)
    {
        var target = new EmailTokenDataValidator();
        var data = new EmailTokenData(Pepper, token, UserName, ValidityPeriod);

        var result = await target.TestValidateAsync(data).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(d => d.Token).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task EmailTokenDataValidator_Token_PatternIsInvalid()
    {
        var target = new EmailTokenDataValidator();
        var data = new EmailTokenData(Pepper, "!@#$", UserName, ValidityPeriod);

        var result = await target.TestValidateAsync(data).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(d => d.Token).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(EmailTokenDataValidator_UserName) + "(null)")]
    [DataRow("", DisplayName = nameof(EmailTokenDataValidator_UserName) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(EmailTokenDataValidator_UserName) + "(WhiteSpace)")]
    public async Task EmailTokenDataValidator_UserName(string userName)
    {
        var target = new EmailTokenDataValidator();
        var data = new EmailTokenData(Pepper, Token, userName, ValidityPeriod);

        var result = await target.TestValidateAsync(data).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.UserName).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailTokenData_Validate()
    {
        var target = new EmailTokenDataValidator();
        var data = new EmailTokenData(Pepper, Token, UserName, ValidityPeriod);

        var result = target.TestValidate(data);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
