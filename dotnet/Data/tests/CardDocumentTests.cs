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

using Hirameku.Common;

[TestClass]
public class CardDocumentTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocument_Constructor()
    {
        var target = new CardDocument();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocument_CreationDate()
    {
        var creationDate = DateTime.UtcNow;

        var target = new CardDocument()
        {
            CreationDate = creationDate,
        };

        Assert.AreEqual(creationDate, target.CreationDate);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocument_Expression()
    {
        const string Expression = nameof(Expression);

        var target = new CardDocument()
        {
            Expression = Expression,
        };

        Assert.AreEqual(Expression, target.Expression);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocument_Id()
    {
        const string Id = nameof(Id);

        var target = new CardDocument()
        {
            Id = Id,
        };

        Assert.AreEqual(Id, target.Id);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocument_Meanings()
    {
        var meanings = new List<Meaning>();

        var target = new CardDocument()
        {
            Meanings = meanings,
        };

        Assert.AreEqual(meanings, target.Meanings);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocument_Notes()
    {
        const string Notes = nameof(Notes);

        var target = new CardDocument()
        {
            Notes = Notes,
        };

        Assert.AreEqual(Notes, target.Notes);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocument_Reading()
    {
        const string Reading = nameof(Reading);

        var target = new CardDocument()
        {
            Reading = Reading,
        };

        Assert.AreEqual(Reading, target.Reading);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CardDocument_Tags()
    {
        var tags = new List<string>();

        var target = new CardDocument()
        {
            Tags = tags,
        };

        Assert.AreEqual(tags, target.Tags);
    }
}
