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
using System.IdentityModel.Tokens.Jwt;

[TestClass]
public class SignInResultTests
{
    private const AuthenticationResult AuthResult = AuthenticationResult.Authenticated;
    private static readonly PersistentTokenModel PersistentToken = new();
    private static readonly JwtSecurityToken SessionToken = new();

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SignInResult_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SignInResult_AuthenticationResult()
    {
        var target = GetTarget();

        Assert.AreEqual(AuthResult, target.AuthenticationResult);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SignInResult_PersistentToken()
    {
        var target = GetTarget();

        Assert.AreEqual(PersistentToken, target.PersistentToken);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SignInResult_SessionToken()
    {
        var target = GetTarget();

        Assert.AreEqual(SessionToken, target.SessionToken);
    }

    private static SignInResult GetTarget()
    {
        return new SignInResult(AuthResult, PersistentToken, SessionToken);
    }
}
