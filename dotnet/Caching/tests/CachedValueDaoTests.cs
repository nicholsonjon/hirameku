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

namespace Hirameku.Caching.Tests;

using Hirameku.Common;
using Hirameku.Data;
using Hirameku.TestTools;
using Moq;
using System.Linq.Expressions;

[TestClass]
public class CachedValueDaoTests
{
    private const UserStatus Status = UserStatus.OK;
    private const string UserId = nameof(UserId);
    private const string ValueKey = CacheSubkeys.UserStatusSubkey + UserId;

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CachedValueDao_Constructor()
    {
        var target = new CachedValueDao(Mock.Of<ICacheClient>(), Mock.Of<IDocumentDao<UserDocument>>());

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task CachedValueDao_GetUserStatus()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await RunGetUserStatusTest(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task CachedValueDao_GetUserStatus_NotCached()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCacheClient = GetMockCacheClient(true, true, true, cancellationToken);
        var mockUserDao = GetMockUserDao(true, cancellationToken);

        await RunGetUserStatusTest(mockCacheClient, mockUserDao, cancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task CachedValueDao_GetUserStatus_UserDoesNotExist()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCacheClient = GetMockCacheClient(true, true, false, cancellationToken);
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        _ = mockUserDao.Setup(m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .ReturnsAsync(null as UserDocument);

        await RunGetUserStatusTest(mockCacheClient, mockUserDao, cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task CachedValueDao_SetUserStatus()
    {
        var mockCacheClient = GetMockCacheClient(isSetVerifiable: true);
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        _ = mockUserDao.Setup(
            m => m.Update(
                UserId,
                It.IsAny<Expression<Func<UserDocument, UserStatus>>>(),
                Status,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        var target = GetTarget(mockCacheClient, mockUserDao);

        await target.SetUserStatus(UserId, Status).ConfigureAwait(false);

        mockCacheClient.Verify();
        mockUserDao.Verify();
    }

    private static Mock<ICacheClient> GetMockCacheClient(
        bool isCacheMiss = false,
        bool isGetVerifiable = false,
        bool isSetVerifiable = false,
        CancellationToken cancellationToken = default)
    {
        var mockCacheClient = new Mock<ICacheClient>();
        var userStatus = Status.ToString();
        var getSetup = mockCacheClient.Setup(m => m.GetValue(ValueKey, cancellationToken));

        if (isCacheMiss)
        {
            _ = getSetup.ReturnsAsync(string.Empty);
        }
        else
        {
            _ = getSetup.ReturnsAsync(userStatus);
        }

        if (isGetVerifiable)
        {
            getSetup.Verifiable();
        }

        var setSetup = mockCacheClient.Setup(m => m.SetValue(ValueKey, userStatus, cancellationToken))
            .Returns(Task.CompletedTask);

        if (isSetVerifiable)
        {
            setSetup.Verifiable();
        }

        return mockCacheClient;
    }

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDao(
        bool verifiable = false,
        CancellationToken cancellationToken = default)
    {
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        var user = GetUser();
        var setup = mockUserDao.Setup(m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, ct) => TestUtilities.AssertExpressionFilter(f, user))
            .ReturnsAsync(user);

        if (verifiable)
        {
            setup.Verifiable();
        }

        return mockUserDao;
    }

    private static CachedValueDao GetTarget(
        Mock<ICacheClient>? mockCacheClient = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default,
        CancellationToken cancellationToken = default)
    {
        mockCacheClient ??= GetMockCacheClient(cancellationToken: cancellationToken);
        mockUserDao ??= GetMockUserDao(cancellationToken: cancellationToken);

        return new CachedValueDao(mockCacheClient.Object, mockUserDao.Object);
    }

    private static UserDocument GetUser()
    {
        return new UserDocument()
        {
            Id = UserId,
            UserStatus = UserStatus.OK,
        };
    }

    private static async Task RunGetUserStatusTest(
        Mock<ICacheClient>? mockCacheClient = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default,
        CancellationToken cancellationToken = default)
    {
        mockCacheClient ??= GetMockCacheClient(cancellationToken: cancellationToken);
        mockUserDao ??= GetMockUserDao(cancellationToken: cancellationToken);
        var target = GetTarget(mockCacheClient, mockUserDao, cancellationToken);

        var userStatus = await target.GetUserStatus(UserId, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(UserStatus.OK, userStatus);
        mockCacheClient.Verify();
        mockUserDao.Verify();
    }
}
