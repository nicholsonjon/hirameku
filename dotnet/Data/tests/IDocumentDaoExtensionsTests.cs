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

using FluentValidation;
using Hirameku.TestTools;
using MongoDB.Driver;
using Moq;
using System.Threading;

[TestClass]
public class IDocumentDaoExtensionsTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task DocumentDao_Fetch_DocumentById()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        const string Id = nameof(Id);
        var expected = new TestDocument(Id);
        var mockCursor = new Mock<IAsyncCursor<TestDocument>>();
        _ = mockCursor.Setup(m => m.MoveNextAsync(cancellationToken))
            .ReturnsAsync(true);
        _ = mockCursor.Setup(m => m.Current)
            .Returns(new List<TestDocument>() { expected });
        _ = mockCursor.Setup(m => m.Dispose());
        var mockCollection = new Mock<IMongoCollection<TestDocument>>();
        _ = mockCollection.Setup(
            m => m.FindAsync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                default(FindOptions<TestDocument, TestDocument>),
                cancellationToken))
            .Callback<FilterDefinition<TestDocument>, FindOptions<TestDocument, TestDocument>, CancellationToken>(
                (f, o, t) => TestUtilities.AssertExpressionFilter(f, expected))
            .ReturnsAsync(mockCursor.Object);
        var target = new DocumentDao<TestDocument>(
            mockCollection.Object,
            new Mock<IValidator<TestDocument>>().Object);

        var actual = await target.Fetch(Id, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expected, actual);
    }
}
