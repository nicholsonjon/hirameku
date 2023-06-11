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

using Hirameku.Common;

[TestClass]
public class AuthenticationEventTests
{
    private const string Accept = nameof(Accept);
    private const AuthenticationResult AuthResult = AuthenticationResult.Authenticated;
    private const string ContentEncoding = nameof(ContentEncoding);
    private const string ContentLanguage = nameof(ContentLanguage);
    private const string Hash = nameof(Hash);
    private const string Id = nameof(Id);
    private const string RemoteIP = nameof(RemoteIP);
    private const string UserAgent = nameof(UserAgent);
    private const string UserId = nameof(UserId);
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEvent_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEvent_Accept()
    {
        var target = GetTarget();

        Assert.AreEqual(Accept, target.Accept);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEvent_AuthenticationResult()
    {
        var target = GetTarget();

        Assert.AreEqual(AuthResult, target.AuthenticationResult);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEvent_ContentEncoding()
    {
        var target = GetTarget();

        Assert.AreEqual(ContentEncoding, target.ContentEncoding);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEvent_ContentLanguage()
    {
        var target = GetTarget();

        Assert.AreEqual(ContentLanguage, target.ContentLanguage);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEvent_Hash()
    {
        var target = GetTarget();

        Assert.AreEqual(Hash, target.Hash);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEvent_Id()
    {
        var target = GetTarget();

        Assert.AreEqual(Id, target.Id);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEvent_RemoteIP()
    {
        var target = GetTarget();

        Assert.AreEqual(RemoteIP, target.RemoteIP);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEvent_UserAgent()
    {
        var target = GetTarget();

        Assert.AreEqual(UserAgent, target.UserAgent);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationEvent_UserId()
    {
        var target = GetTarget();

        Assert.AreEqual(UserId, target.UserId);
    }

    private static AuthenticationEvent GetTarget()
    {
        return new AuthenticationEvent()
        {
            Accept = Accept,
            AuthenticationResult = AuthResult,
            ContentEncoding = ContentEncoding,
            ContentLanguage = ContentLanguage,
            Hash = Hash,
            Id = Id,
            RemoteIP = RemoteIP,
            UserAgent = UserAgent,
            UserId = UserId,
        };
    }
}
