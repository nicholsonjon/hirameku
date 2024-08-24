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

namespace Hirameku.User.Tests;

using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Moq;
using System.Security.Claims;

[TestClass]
public class DeleteUserHandlerTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DeleteUserHandler_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task DeleteUserHandler_DeleteUser()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        const string UserId = nameof(UserId);
        mockUserDao.Setup(m => m.Delete(UserId, cancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var target = GetTarget(mockUserDao: mockUserDao);
        var model = new Authenticated<Unit>(
            Unit.Value,
            new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, UserId) })));

        await target.DeleteUser(model, cancellationToken).ConfigureAwait(false);

        mockUserDao.Verify();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task DeleteUserHandler_DeleteUser_AuthenticatedModelIsNull_Throws()
    {
        var target = GetTarget();

        await target.DeleteUser(null!).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow("", DisplayName = nameof(DeleteUserHandler_DeleteUser_UserIdIsEmptyOrWhiteSpace_Throws) + "(string.Empty)")]
    [DataRow("\t\r\n ", DisplayName = nameof(DeleteUserHandler_DeleteUser_UserIdIsEmptyOrWhiteSpace_Throws) + "(WhiteSpace)")]
    public async Task DeleteUserHandler_DeleteUser_UserIdIsEmptyOrWhiteSpace_Throws(string userId)
    {
        var target = GetTarget();
        var model = new Authenticated<Unit>(
            Unit.Value,
            new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, userId) })));

        await target.DeleteUser(model).ConfigureAwait(false);
    }

    private static DeleteUserHandler GetTarget(Mock<IDocumentDao<UserDocument>>? mockUserDao = default)
    {
        return new DeleteUserHandler(mockUserDao?.Object ?? Mock.Of<IDocumentDao<UserDocument>>());
    }
}
