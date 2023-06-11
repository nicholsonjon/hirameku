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

namespace Hirameku.Common.Tests;

[TestClass]
public class UserTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void User_Constructor()
    {
        var target = new User();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void User_EmailAddress()
    {
        const string EmailAddress = nameof(EmailAddress);

        var target = new User()
        {
            EmailAddress = EmailAddress,
        };

        Assert.AreEqual(EmailAddress, target.EmailAddress);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void User_Name()
    {
        const string Name = nameof(Name);

        var target = new User()
        {
            Name = Name,
        };

        Assert.AreEqual(Name, target.Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void User_UserName()
    {
        const string UserName = nameof(UserName);

        var target = new User()
        {
            UserName = UserName,
        };

        Assert.AreEqual(UserName, target.UserName);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void User_UserStatus()
    {
        const UserStatus UserStatus = UserStatus.OK;

        var target = new User()
        {
            UserStatus = UserStatus,
        };

        Assert.AreEqual(UserStatus, target.UserStatus);
    }
}
