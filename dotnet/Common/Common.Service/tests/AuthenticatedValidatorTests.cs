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

namespace Hirameku.Common.Service.Tests;

using FluentValidation;
using FluentValidation.TestHelper;
using System.Security.Claims;

[TestClass]
public class AuthenticatedValidatorTests
{
    private const string UserId = nameof(UserId);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticatedValidator_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(AuthenticatedValidator_User_Claims_UserId_NullEmptyOrWhiteSpace) + "(null)")]
    [DataRow("", DisplayName = nameof(AuthenticatedValidator_User_Claims_UserId_NullEmptyOrWhiteSpace) + "(string.Empty)")]
    [DataRow("\t\r\n ", DisplayName = nameof(AuthenticatedValidator_User_Claims_UserId_NullEmptyOrWhiteSpace) + "(WhiteSpace)")]
    public async Task AuthenticatedValidator_User_Claims_UserId_NullEmptyOrWhiteSpace(string userId)
    {
        var model = new Authenticated<TestModel>(new TestModel(), GetMockPrincipal(userId));
        var target = GetTarget();

        var result = await target.TestValidateAsync(model).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.User.Claims).Only();
    }

    private static ClaimsPrincipal GetMockPrincipal(string userId = UserId)
    {
        var identity = userId != null
            ? new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, userId) })
            : new ClaimsIdentity();

        return new ClaimsPrincipal(identity);
    }

    private static AuthenticatedValidator<TestModel> GetTarget()
    {
        return new AuthenticatedValidator<TestModel>(new TestModelValidator());
    }

    private sealed class TestModel
    {
        public TestModel()
        {
        }
    }

    private sealed class TestModelValidator : AbstractValidator<TestModel>
    {
        public TestModelValidator()
        {
        }
    }
}
