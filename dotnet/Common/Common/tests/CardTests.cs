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
public class CardTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Card_Constructor()
    {
        var target = new Card();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Card_CreationDate()
    {
        var creationDate = DateTime.UtcNow;

        var target = new Card()
        {
            CreationDate = creationDate,
        };

        Assert.AreEqual(creationDate, target.CreationDate);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Card_Expression()
    {
        const string Expression = nameof(Expression);

        var target = new Card()
        {
            Expression = Expression,
        };

        Assert.AreEqual(Expression, target.Expression);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Card_Meanings()
    {
        var meanings = new List<Meaning>();

        var target = new Card()
        {
            Meanings = meanings,
        };

        Assert.AreEqual(meanings, target.Meanings);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Card_Notes()
    {
        const string Notes = nameof(Notes);

        var target = new Card()
        {
            Notes = Notes,
        };

        Assert.AreEqual(Notes, target.Notes);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Card_Reading()
    {
        const string Reading = nameof(Reading);

        var target = new Card()
        {
            Reading = Reading,
        };

        Assert.AreEqual(Reading, target.Reading);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Card_Tags()
    {
        var tags = new List<string>();

        var target = new Card()
        {
            Tags = tags,
        };

        Assert.AreEqual(tags, target.Tags);
    }
}
