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
using Hirameku.TestTools;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;

[TestClass]
public class GetUserHandlerTests
{
    private const string UserId = "1234567890abcdef12345678";

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void GetUserHandler_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task GetUserHandler_GetUser()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var expected = new UserDocument() { Id = UserId };
        var target = GetTarget(mockUserDao: GetMockUserDao(expected, cancellationToken));
        var model = new Authenticated<Unit>(
            Unit.Value,
            new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, UserId) })));

        var actual = await target.GetUser(model, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task GetUserHandler_GetUser_AuthenticatedModelIsNull_Throws()
    {
        var target = GetTarget();

        _ = await target.GetUser(null!).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow("", DisplayName = nameof(GetUserHandler_GetUser_UserIdEmptyOrWhiteSpace_Throws) + "(string.Empty)")]
    [DataRow("\t\r\n ", DisplayName = nameof(GetUserHandler_GetUser_UserIdEmptyOrWhiteSpace_Throws) + "(WhiteSpace)")]
    public async Task GetUserHandler_GetUser_UserIdEmptyOrWhiteSpace_Throws(string userId)
    {
        var target = GetTarget();
        var model = new Authenticated<Unit>(
            Unit.Value,
            new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, userId) })));

        _ = await target.GetUser(model).ConfigureAwait(false);
    }

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDao(
        UserDocument? user = default,
        CancellationToken cancellationToken = default)
    {
        var mockDao = new Mock<IDocumentDao<UserDocument>>();
        mockDao.Setup(m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, _) => TestUtilities.AssertExpressionFilter(f, user ?? GetUser()))
            .ReturnsAsync(user)
            .Verifiable();

        return mockDao;
    }

    private static GetUserHandler GetTarget(Mock<IDocumentDao<UserDocument>>? mockUserDao = default)
    {
        return new GetUserHandler(mockUserDao?.Object ?? Mock.Of<IDocumentDao<UserDocument>>());
    }

    private static UserDocument GetUser()
    {
        return new UserDocument() { Id = UserId };
    }
}
