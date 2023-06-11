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

using Newtonsoft.Json;

[TestClass]
public class RegisterModelTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegisterModel_Constructor()
    {
        var target = new RegisterModel();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegisterModel_EmailAddress()
    {
        const string EmailAddress = nameof(EmailAddress);

        var target = new RegisterModel()
        {
            EmailAddress = EmailAddress,
        };

        Assert.AreEqual(EmailAddress, target.EmailAddress);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegisterModel_JsonSerialization_PasswordIsIgnored()
    {
        const string Password = nameof(Password);
        var target = new RegisterModel() { Password = Password };

        var serialized = JsonConvert.SerializeObject(target);
        var deserialized = JsonConvert.DeserializeObject<RegisterModel>(serialized);

        Assert.IsNotNull(deserialized);
        Assert.IsTrue(string.IsNullOrEmpty(deserialized!.Password));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegisterModel_Name()
    {
        const string Name = nameof(Name);

        var target = new RegisterModel()
        {
            Name = Name,
        };

        Assert.AreEqual(Name, target.Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegisterModel_Password()
    {
        const string Password = nameof(Password);

        var target = new RegisterModel()
        {
            Password = Password,
        };

        Assert.AreEqual(Password, target.Password);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegisterModel_RecaptchaResponse()
    {
        const string RecaptchaResponse = nameof(RecaptchaResponse);

        var target = new RegisterModel()
        {
            RecaptchaResponse = RecaptchaResponse,
        };

        Assert.AreEqual(RecaptchaResponse, target.RecaptchaResponse);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegisterModel_UserName()
    {
        const string UserName = nameof(UserName);

        var target = new RegisterModel()
        {
            UserName = UserName,
        };

        Assert.AreEqual(UserName, target.UserName);
    }
}
