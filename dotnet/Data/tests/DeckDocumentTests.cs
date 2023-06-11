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
public class DeckDocumentTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DeckDocument_Constructor()
    {
        var target = new DeckDocument();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DeckDocument_CreationDate()
    {
        var creationDate = DateTime.UtcNow;

        var target = new DeckDocument()
        {
            CreationDate = creationDate,
        };

        Assert.AreEqual(creationDate, target.CreationDate);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DeckDocument_Id()
    {
        const string Id = nameof(Id);

        var target = new DeckDocument()
        {
            Id = Id,
        };

        Assert.AreEqual(Id, target.Id);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DeckDocument_Name()
    {
        const string Name = nameof(Name);

        var target = new DeckDocument()
        {
            Name = Name,
        };

        Assert.AreEqual(Name, target.Name);
    }
}
