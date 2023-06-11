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

[TestClass]
public class ChangePasswordModelTests
{
    private const string CurrentPassword = nameof(CurrentPassword);
    private const string NewPassword = nameof(NewPassword);
    private const bool RememberMe = true;

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ChangePasswordModel_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ChangePasswordModel_CurrentPassword()
    {
        var target = GetTarget();

        Assert.AreEqual(CurrentPassword, target.CurrentPassword);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ChangePasswordModel_NewPassword()
    {
        var target = GetTarget();

        Assert.AreEqual(NewPassword, target.NewPassword);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ChangePasswordModel_RememberMe()
    {
        var target = GetTarget();

        Assert.AreEqual(RememberMe, target.RememberMe);
    }

    private static ChangePasswordModel GetTarget()
    {
        return new ChangePasswordModel()
        {
            CurrentPassword = CurrentPassword,
            NewPassword = NewPassword,
            RememberMe = RememberMe,
        };
    }
}
