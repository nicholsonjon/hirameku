﻿// Hirameku is a cloud-native, vendor-agnostic, serverless application for
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

using Newtonsoft.Json;

[TestClass]
public class SignInModelTests
{
    private const string Password = nameof(Password);
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SignInModel_Constructor()
    {
        var target = new SignInModel();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SignInModel_JsonSerialization_PasswordIsIgnored()
    {
        var target = GetTarget();

        var serialized = JsonConvert.SerializeObject(target);
        var deserialized = JsonConvert.DeserializeObject<SignInModel>(serialized);

        Assert.IsNotNull(deserialized);
        Assert.IsTrue(string.IsNullOrEmpty(deserialized!.Password));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SignInModel_Password()
    {
        var model = GetTarget();

        Assert.AreEqual(Password, model.Password);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SignInModel_RememberMe()
    {
        var model = GetTarget();

        Assert.IsTrue(model.RememberMe);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SignInModel_UserName()
    {
        var model = GetTarget();

        Assert.AreEqual(UserName, model.UserName);
    }

    private static SignInModel GetTarget()
    {
        return new SignInModel()
        {
            Password = Password,
            RememberMe = true,
            UserName = UserName,
        };
    }
}
