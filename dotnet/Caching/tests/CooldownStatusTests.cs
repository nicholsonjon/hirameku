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

namespace Hirameku.Caching.Tests;

[TestClass]
public class CooldownStatusTests
{
    private const bool IsOnCooldown = true;
    private static readonly DateTime ExpireTime = DateTime.UtcNow;
    private static readonly TimeSpan TimeToLive = new(0, 5, 0);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CooldownStatus_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CooldownStatus_ExpireTime()
    {
        var target = GetTarget();

        Assert.AreEqual(ExpireTime, target.ExpireTime);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CooldownStatus_IsOnCooldown()
    {
        var target = GetTarget();

        Assert.AreEqual(IsOnCooldown, target.IsOnCooldown);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CooldownStatus_TimeToLive()
    {
        var target = GetTarget();

        Assert.AreEqual(TimeToLive, target.TimeToLive);
    }

    private static CooldownStatus GetTarget()
    {
        return new CooldownStatus(ExpireTime, IsOnCooldown, TimeToLive);
    }
}
