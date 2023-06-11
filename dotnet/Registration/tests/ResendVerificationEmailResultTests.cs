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

[TestClass]
public class ResendVerificationEmailResultTests
{
    private const bool WasResent = true;
    private static readonly TimeSpan CooldownDuration = new(0, 5, 0);
    private static readonly DateTime? CooldownExpirationTime = DateTime.UtcNow;

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResendVerificationEmailResult_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResendVerificationEmailResult_CooldownDuration()
    {
        var target = GetTarget();

        Assert.AreEqual(CooldownDuration, target.CooldownDuration);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResendVerificationEmailResult_CooldownExpirationTime()
    {
        var target = GetTarget();

        Assert.AreEqual(CooldownExpirationTime, target.CooldownExpirationTime);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResendVerificationEmailResult_WasResent()
    {
        var target = GetTarget();

        Assert.AreEqual(WasResent, target.WasResent);
    }

    private static ResendVerificationEmailResult GetTarget()
    {
        return new ResendVerificationEmailResult(CooldownDuration, CooldownExpirationTime, WasResent);
    }
}
