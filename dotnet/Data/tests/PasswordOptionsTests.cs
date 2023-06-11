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

[TestClass]
public class PasswordOptionsTests
{
    private const bool DisallowSavingIdenticalPasswords = true;
    private static readonly TimeSpan MaxPasswordAge = TimeSpan.FromDays(365);
    private static readonly TimeSpan MinPasswordAge = TimeSpan.FromMinutes(5);
    private static readonly PasswordHashVersion Version = PasswordHashVersion.Current;

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordOptions_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordOptions_GetDisallowSavingIdenticalPasswords()
    {
        var target = GetTarget();

        Assert.AreEqual(DisallowSavingIdenticalPasswords, target.DisallowSavingIdenticalPasswords);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordOptions_GetMaxPasswordAge()
    {
        var target = GetTarget();

        Assert.AreEqual(MaxPasswordAge, target.MaxPasswordAge);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordOptions_GetMinPasswordAge()
    {
        var target = GetTarget();

        Assert.AreEqual(MinPasswordAge, target.MinPasswordAge);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordOptions_GetPasswordHashVersion()
    {
        var target = GetTarget();

        Assert.AreEqual(Version, target.Version);
    }

    private static PasswordOptions GetTarget()
    {
        return new PasswordOptions()
        {
            DisallowSavingIdenticalPasswords = DisallowSavingIdenticalPasswords,
            MaxPasswordAge = MaxPasswordAge,
            MinPasswordAge = MinPasswordAge,
            Version = Version,
        };
    }
}
