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

using FluentValidation;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.TestTools;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;

[TestClass]
public class UpdateUserNameHandlerTests
{
    private const string UserId = "1234567890abcdef12345678";
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UpdateUserNameHandler_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UpdateUserNameHandler_UpdateUserName()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var user = new UserDocument() { Id = UserId };
        var mockUserDao = GetMockUserDaoForUpdateUserName(user, cancellationToken);
        var expected = new JwtSecurityToken();
        var mockSecurityTokenIssuer = GetMockSecurityTokenIssuer(user, expected);
        var target = GetTarget(mockUserDao: mockUserDao, mockSecurityTokenIssuer: mockSecurityTokenIssuer);
        var model = new Authenticated<UpdateUserNameModel>(
            new UpdateUserNameModel() { UserName = UserName },
            new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, UserId) })));

        var actual = await target.UpdateUserName(model, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expected, actual);
        mockUserDao.Verify();
        mockSecurityTokenIssuer.Verify();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task UpdateUserNameHandler_UpdateUserName_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        _ = await target.UpdateUserName(
            new Authenticated<UpdateUserNameModel>(new UpdateUserNameModel(), new ClaimsPrincipal()))
            .ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task UpdateUserNameHandler_UpdateUserName_UserDoesNotExist_Throws()
    {
        var target = GetTarget();

        _ = await target.UpdateUserName(
            new Authenticated<UpdateUserNameModel>(
                new UpdateUserNameModel() { UserName = UserName },
                new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, "foo") }))))
            .ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    private static Mock<ISecurityTokenIssuer> GetMockSecurityTokenIssuer(User user, SecurityToken sessionToken)
    {
        var mockIssuer = new Mock<ISecurityTokenIssuer>();
        mockIssuer.Setup(m => m.Issue(UserId, user))
            .Returns(sessionToken ?? new JwtSecurityToken())
            .Verifiable();

        return mockIssuer;
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

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDaoForUpdateUserName(
        UserDocument? user = default,
        CancellationToken cancellationToken = default)
    {
        var mockDao = GetMockUserDao(user, cancellationToken);
        mockDao.Setup(
            m => m.Update(UserId, It.IsAny<Expression<Func<UserDocument, string>>>(), UserName, cancellationToken))
            .Callback<string, Expression<Func<UserDocument, string>>, string, CancellationToken>(
                (id, f, v, _) =>
                {
                    Assert.AreEqual(UserId, id);
                    TestUtilities.AssertMemberExpression(f, nameof(UserDocument.UserName));
                    Assert.AreEqual(UserName, v);
                })
            .Returns(Task.CompletedTask)
            .Verifiable();

        return mockDao;
    }

    private static UpdateUserNameHandler GetTarget(
        Mock<ISecurityTokenIssuer>? mockSecurityTokenIssuer = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default)
    {
        return new UpdateUserNameHandler(
            mockSecurityTokenIssuer?.Object ?? Mock.Of<ISecurityTokenIssuer>(),
            new AuthenticatedValidator<UpdateUserNameModel>(new UpdateUserNameModelValidator()),
            mockUserDao?.Object ?? Mock.Of<IDocumentDao<UserDocument>>());
    }

    private static UserDocument GetUser()
    {
        return new UserDocument()
        {
            Id = UserId,
            UserName = UserName,
        };
    }
}
