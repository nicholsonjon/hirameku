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
public class AuthenticationEventValidatorTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEventValidator_Constructor()
    {
        var target = new AuthenticationEventValidator();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(AuthenticationResult.Authenticated)]
    [DataRow(AuthenticationResult.LockedOut)]
    [DataRow(AuthenticationResult.NotAuthenticated)]
    [DataRow(AuthenticationResult.PasswordExpired)]
    [DataRow(AuthenticationResult.Suspended)]
    public void AuthenticationEventValidator_AuthenticationResult(AuthenticationResult authenticationResult)
    {
        var target = new AuthenticationEventValidator();
        var authenticationEvent = GetValidAuthenticationEvent();
        authenticationEvent.AuthenticationResult = authenticationResult;

        target.TestValidate(authenticationEvent).ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEventValidator_AuthenticationResult_IsInvalid()
    {
        var target = new AuthenticationEventValidator();
        var authenticationEvent = GetValidAuthenticationEvent();
        authenticationEvent.AuthenticationResult = (AuthenticationResult)(-1);

        _ = target.TestValidate(authenticationEvent)
            .ShouldHaveValidationErrorFor(e => e.AuthenticationResult)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(AuthenticationEventValidator_Hash_IsNullEmptyOrWhiteSpace) + "(null)")]
    [DataRow("", DisplayName = nameof(AuthenticationEventValidator_Hash_IsNullEmptyOrWhiteSpace) + "(string.Empty)")]
    [DataRow("\t\r\n ", DisplayName = nameof(AuthenticationEventValidator_Hash_IsNullEmptyOrWhiteSpace) + "(WhiteSpace)")]
    public void AuthenticationEventValidator_Hash_IsNullEmptyOrWhiteSpace(string hash)
    {
        var target = new AuthenticationEventValidator();
        var authenticationEvent = GetValidAuthenticationEvent();
        authenticationEvent.Hash = hash;

        _ = target.TestValidate(authenticationEvent)
            .ShouldHaveValidationErrorFor(e => e.Hash)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEventValidator_Id_LengthIsInvalid()
    {
        var target = new AuthenticationEventValidator();
        var authenticationEvent = GetValidAuthenticationEvent();
        authenticationEvent.Id = "1";

        _ = target.TestValidate(authenticationEvent)
            .ShouldHaveValidationErrorFor(c => c.Id)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEventValidator_Id_PatternIsInvalid()
    {
        var target = new AuthenticationEventValidator();
        var authenticationEvent = GetValidAuthenticationEvent();
        var random = new Faker().Random;
        authenticationEvent.Id = random.String(Constants.ValidIdLength, 'g', 'z');

        _ = target.TestValidate(authenticationEvent)
            .ShouldHaveValidationErrorFor(c => c.Id)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(AuthenticationEventValidator_RemoteIP_IsNullEmptyOrWhiteSpace) + "(null)")]
    [DataRow("", DisplayName = nameof(AuthenticationEventValidator_RemoteIP_IsNullEmptyOrWhiteSpace) + "(string.Empty)")]
    [DataRow("\t\r\n ", DisplayName = nameof(AuthenticationEventValidator_RemoteIP_IsNullEmptyOrWhiteSpace) + "(WhiteSpace)")]
    public void AuthenticationEventValidator_RemoteIP_IsNullEmptyOrWhiteSpace(string remoteIP)
    {
        var target = new AuthenticationEventValidator();
        var authenticationEvent = GetValidAuthenticationEvent();
        authenticationEvent.RemoteIP = remoteIP;

        _ = target.TestValidate(authenticationEvent)
            .ShouldHaveValidationErrorFor(e => e.RemoteIP)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEventValidator_UserId_LengthIsInvalid()
    {
        var target = new AuthenticationEventValidator();
        var authenticationEvent = GetValidAuthenticationEvent();
        authenticationEvent.UserId = "1";

        _ = target.TestValidate(authenticationEvent)
            .ShouldHaveValidationErrorFor(c => c.UserId)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEventValidator_UserId_PatternIsInvalid()
    {
        var target = new AuthenticationEventValidator();
        var authenticationEvent = GetValidAuthenticationEvent();
        var random = new Faker().Random;
        authenticationEvent.UserId = random.String(Constants.ValidIdLength, 'g', 'z');

        _ = target.TestValidate(authenticationEvent)
            .ShouldHaveValidationErrorFor(c => c.UserId)
            .Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow("")]
    [DataRow("1234567890abcdef12345678")]
    public void AuthenticationEventValidator_Validate(string id)
    {
        var target = new AuthenticationEventValidator();
        var authenticationEvent = GetValidAuthenticationEvent();
        authenticationEvent.Id = id;

        target.TestValidate(authenticationEvent).ShouldNotHaveAnyValidationErrors();
    }

    private static AuthenticationEvent GetValidAuthenticationEvent()
    {
        var faker = new Faker();
        var random = faker.Random;

        return new AuthenticationEvent()
        {
            Hash = random.Word(),
            Id = random.Hexadecimal(Constants.ValidIdLength, string.Empty),
            RemoteIP = random.Word(),
            UserId = random.Hexadecimal(Constants.ValidIdLength, string.Empty),
        };
    }
}
