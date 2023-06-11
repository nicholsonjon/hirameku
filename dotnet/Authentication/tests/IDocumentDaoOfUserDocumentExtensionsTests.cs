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

using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Moq;
using System.Linq.Expressions;

[TestClass]
public class IDocumentDaoOfUserDocumentExtensionsTests
{
    private const string UserId = nameof(UserId);
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task IDocumentDaoOfUserDocumentExtensions_GetUser()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var expected = GetUser(UserStatus.OK);
        var mockUserDao = GetMockUserDao(expected, cancellationToken);
        var target = mockUserDao.Object;

        var actual = await target.GetUserByUserName(UserName, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task IDocumentDaoOfUserDocumentExtensions_GetUserById_InstanceIsNull_Throws()
    {
        _ = await IDocumentDaoOfUserDocumentExtensions.GetUserById(null!, UserId).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task IDocumentDaoOfUserDocumentExtensions_GetUserById_UserDoesNotExist_Throws()
    {
        var mockUserDao = GetMockUserDao(null!);
        var target = mockUserDao.Object;

        _ = await target.GetUserById(UserId).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserSuspendedException))]
    public async Task IDocumentDaoOfUserDocumentExtensions_GetUserById_UserSuspended_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var user = GetUser(UserStatus.Suspended);
        var mockUserDao = GetMockUserDao(user, cancellationToken);
        var target = mockUserDao.Object;

        _ = await target.GetUserById(UserId, cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(UserSuspendedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task IDocumentDaoOfUserDocumentExtensions_GetUserByUserName_InstanceIsNull_Throws()
    {
        _ = await IDocumentDaoOfUserDocumentExtensions.GetUserByUserName(null!, UserName).ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task IDocumentDaoOfUserDocumentExtensions_GetUserByUserName_UserDoesNotExist_Throws()
    {
        var mockUserDao = GetMockUserDao(null!);
        var target = mockUserDao.Object;

        _ = await target.GetUserByUserName(UserName).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserSuspendedException))]
    public async Task IDocumentDaoOfUserDocumentExtensions_GetUserByUserName_UserSuspended_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var user = GetUser(UserStatus.Suspended);
        var mockUserDao = GetMockUserDao(user, cancellationToken);
        var target = mockUserDao.Object;

        _ = await target.GetUserByUserName(UserName, cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(UserSuspendedException) + " expected");
    }

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDao(
        UserDocument user,
        CancellationToken cancellationToken = default)
    {
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        _ = mockUserDao.Setup(m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .ReturnsAsync(user);

        return mockUserDao;
    }

    private static UserDocument GetUser(UserStatus userStatus = UserStatus.OK)
    {
        return new UserDocument()
        {
            UserName = UserName,
            UserStatus = userStatus,
        };
    }
}
