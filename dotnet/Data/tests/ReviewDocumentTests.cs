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
public class ReviewDocumentTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewDocument_Constructor()
    {
        var target = new ReviewDocument();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewDocument_CardId()
    {
        const string CardId = nameof(CardId);

        var target = new ReviewDocument()
        {
            CardId = CardId,
        };

        Assert.AreEqual(CardId, target.CardId);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewDocument_Disposition()
    {
        var diposition = Disposition.Remembered;

        var target = new ReviewDocument()
        {
            Disposition = diposition,
        };

        Assert.AreEqual(diposition, target.Disposition);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewDocument_Id()
    {
        const string Id = nameof(Id);

        var target = new ReviewDocument()
        {
            Id = Id,
        };

        Assert.AreEqual(Id, target.Id);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewDocument_Interval()
    {
        var interval = Interval.Beginner2;

        var target = new ReviewDocument()
        {
            Interval = interval,
        };

        Assert.AreEqual(interval, target.Interval);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewDocument_ReviewDate()
    {
        var reviewDate = DateTime.UtcNow;

        var target = new ReviewDocument()
        {
            ReviewDate = reviewDate,
        };

        Assert.AreEqual(reviewDate, target.ReviewDate);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReviewDocument_UserId()
    {
        const string UserId = nameof(UserId);

        var target = new ReviewDocument()
        {
            UserId = UserId,
        };

        Assert.AreEqual(UserId, target.UserId);
    }
}
