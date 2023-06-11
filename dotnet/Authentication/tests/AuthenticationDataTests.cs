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

[TestClass]
public class AuthenticationDataTests
{
    private const string Accept = nameof(Accept);
    private const string ContentEncoding = nameof(ContentEncoding);
    private const string ContentLanguage = nameof(ContentLanguage);
    private const string RemoteIP = nameof(RemoteIP);
    private const string UserAgent = nameof(UserAgent);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationData_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationData_Accept()
    {
        var target = GetTarget();

        Assert.AreEqual(Accept, target.Accept);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationData_ContentEncoding()
    {
        var target = GetTarget();

        Assert.AreEqual(ContentEncoding, target.ContentEncoding);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationData_ContentLanguage()
    {
        var target = GetTarget();

        Assert.AreEqual(ContentLanguage, target.ContentLanguage);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationData_Model()
    {
        var model = new object();
        var target = GetTarget(model);

        Assert.AreEqual(model, target.Model);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationData_RemoteIP()
    {
        var target = GetTarget();

        Assert.AreEqual(RemoteIP, target.RemoteIP);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationData_UserAgent()
    {
        var target = GetTarget();

        Assert.AreEqual(UserAgent, target.UserAgent);
    }

    private static AuthenticationData<object> GetTarget(object? model = default)
    {
        return new AuthenticationData<object>(
            Accept,
            ContentEncoding,
            ContentLanguage,
            model ?? new object(),
            RemoteIP,
            UserAgent);
    }
}
