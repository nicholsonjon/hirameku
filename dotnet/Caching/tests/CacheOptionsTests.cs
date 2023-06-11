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
public class CacheOptionsTests
{
    private const string ConnectionString = nameof(ConnectionString);
    private const int DatabaseNumber = -1;
    private static readonly TimeSpan CooldownTimeToLive = new(0, 5, 0);
    private static readonly TimeSpan CounterTimeToLive = new(0, 5, 0);
    private static readonly TimeSpan OperationTimeout = new(0, 0, 5);
    private static readonly TimeSpan ValueTimeToLive = new(0, 5, 0);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CacheOptions_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CacheOptions_ConnectionString()
    {
        var target = GetTarget();

        Assert.AreEqual(ConnectionString, target.ConnectionString);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CacheOptions_CooldownTimeToLive()
    {
        var target = GetTarget();

        Assert.AreEqual(CooldownTimeToLive, target.CooldownTimeToLive);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CacheOptions_CounterTimeToLive()
    {
        var target = GetTarget();

        Assert.AreEqual(CounterTimeToLive, target.CounterTimeToLive);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CacheOptions_DatabaseNumber()
    {
        var target = GetTarget();

        Assert.AreEqual(DatabaseNumber, target.DatabaseNumber);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CacheOptions_OperationTimeout()
    {
        var target = GetTarget();

        Assert.AreEqual(OperationTimeout, target.OperationTimeout);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CacheOptions_ValueTimeToLive()
    {
        var target = GetTarget();

        Assert.AreEqual(ValueTimeToLive, target.ValueTimeToLive);
    }

    private static CacheOptions GetTarget()
    {
        return new CacheOptions()
        {
            ConnectionString = ConnectionString,
            CooldownTimeToLive = CooldownTimeToLive,
            CounterTimeToLive = CounterTimeToLive,
            DatabaseNumber = DatabaseNumber,
            OperationTimeout = OperationTimeout,
            ValueTimeToLive = ValueTimeToLive,
        };
    }
}
