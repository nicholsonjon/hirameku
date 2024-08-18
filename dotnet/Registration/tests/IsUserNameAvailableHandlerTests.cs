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

namespace Hirameku.Registration.Tests;

using Hirameku.Common;
using Hirameku.Data;
using Hirameku.TestTools;
using Moq;
using System.Linq.Expressions;

[TestClass]
public class IsUserNameAvailableHandlerTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IsUserNameAvailableHandler_Constructor()
    {
        var target = new IsUserNameAvailableHandler(Mock.Of<IDocumentDao<UserDocument>>());

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task IsUserNameAvailableHandler_IsUserNameAvailable()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var user = GetUser();
        var mockUserDao = GetMockUserDao(user, cancellationToken);
        var target = new IsUserNameAvailableHandler(mockUserDao.Object);

        var isUserNameAvailable = await target.IsUserNameAvailable(user.UserName, cancellationToken)
            .ConfigureAwait(false);

        Assert.IsTrue(isUserNameAvailable);
        mockUserDao.Verify();
    }

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDao(
        UserDocument user,
        CancellationToken cancellationToken)
    {
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();

        mockUserDao.Setup(m => m.GetCount(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, ct) => TestUtilities.AssertExpressionFilter(f, user))
            .ReturnsAsync(0)
            .Verifiable();

        return mockUserDao;
    }

    private static UserDocument GetUser()
    {
        return new UserDocument()
        {
            EmailAddress = "test@test.local",
            Id = nameof(UserDocument.Id),
            UserName = nameof(UserDocument.UserName),
            UserStatus = UserStatus.OK,
        };
    }
}
