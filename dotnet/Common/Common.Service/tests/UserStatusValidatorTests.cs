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

using Hirameku.Caching;
using Moq;
using System.Security.Claims;

[TestClass]
public class UserStatusValidatorTests
{
    private const string UserId = nameof(UserId);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserStatusValidator_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserStatusValidator_ValidateUserStatus()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var target = GetTarget(GetMockCachedValueDao(cancellationToken: cancellationToken));

        await target.ValidateUserStatus(GetUser(), cancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(EmailAddressNotVerifiedException))]
    public async Task UserStatusValidator_ValidateUserStatus_EmailNotVerified_Throws()
    {
        var target = GetTarget(GetMockCachedValueDao(UserStatus.EmailNotVerified));

        await target.ValidateUserStatus(GetUser()).ConfigureAwait(false);

        Assert.Fail(nameof(EmailAddressNotVerifiedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserMustChangePasswordException))]
    [DataRow(UserStatus.EmailNotVerifiedAndPasswordChangeRequired)]
    [DataRow(UserStatus.PasswordChangeRequired)]
    public async Task UserStatusValidator_ValidateUserStatus_PasswordChangeRequired_Throws(UserStatus userStatus)
    {
        var target = GetTarget(GetMockCachedValueDao(userStatus));

        await target.ValidateUserStatus(GetUser()).ConfigureAwait(false);

        Assert.Fail(nameof(UserMustChangePasswordException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserSuspendedException))]
    public async Task UserStatusValidator_ValidateUserStatus_Suspended_Throws()
    {
        var target = GetTarget(GetMockCachedValueDao(UserStatus.Suspended));

        await target.ValidateUserStatus(GetUser()).ConfigureAwait(false);

        Assert.Fail(nameof(EmailAddressNotVerifiedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow("", DisplayName = nameof(UserStatusValidator_ValidateUserStatus_UserIdEmptyOrWhiteSpace_Throws) + "(string.Empty)")]
    [DataRow("\t\r\n ", DisplayName = nameof(UserStatusValidator_ValidateUserStatus_UserIdEmptyOrWhiteSpace_Throws) + "(WhiteSpace)")]
    public async Task UserStatusValidator_ValidateUserStatus_UserIdEmptyOrWhiteSpace_Throws(string userId)
    {
        var target = GetTarget();

        await target.ValidateUserStatus(GetUser(userId)).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentException) + " expected");
    }

    private static Mock<ICachedValueDao> GetMockCachedValueDao(
        UserStatus userStatus = UserStatus.OK,
        CancellationToken cancellationToken = default)
    {
        var mockDao = new Mock<ICachedValueDao>();
        _ = mockDao.Setup(m => m.GetUserStatus(UserId, cancellationToken))
            .ReturnsAsync(userStatus);

        return mockDao;
    }

    private static UserStatusValidator GetTarget(Mock<ICachedValueDao>? mockCachedValueDao = default)
    {
        return new UserStatusValidator(mockCachedValueDao?.Object ?? Mock.Of<ICachedValueDao>());
    }

    private static ClaimsPrincipal GetUser(string? userId = default)
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity(new Claim[] { new Claim(PrivateClaims.UserId, userId ?? UserId) }));
    }
}
