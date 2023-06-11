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
public class DatabaseOptionsTests
{
    private const string ConnectionString = nameof(ConnectionString);
    private const string DatabaseName = nameof(DatabaseName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DatabaseOptions_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DatabaseOptions_ConnectionString()
    {
        var target = GetTarget();

        Assert.AreEqual(ConnectionString, target.ConnectionString);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DatabaseOptions_DatabaseName()
    {
        var target = GetTarget();

        Assert.AreEqual(DatabaseName, target.DatabaseName);
    }

    private static DatabaseOptions GetTarget()
    {
        return new DatabaseOptions()
        {
            ConnectionString = ConnectionString,
            DatabaseName = DatabaseName,
        };
    }
}
