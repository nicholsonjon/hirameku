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
public class DocumentDaoTests
{
    private const string Id = nameof(Id);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DocumentDao_Constructor()
    {
        var target = new DocumentDao<TestDocument>(
            new Mock<IMongoCollection<TestDocument>>().Object,
            new Mock<IValidator<TestDocument>>().Object);

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task DocumentDao_Delete()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCollection = new Mock<IMongoCollection<TestDocument>>();
        _ = mockCollection.Setup(
            m => m.DeleteOneAsync(It.IsAny<ExpressionFilterDefinition<TestDocument>>(), cancellationToken))
            .Callback<FilterDefinition<TestDocument>, CancellationToken>(
                (f, t) => TestUtilities.AssertExpressionFilter(f, new TestDocument(Id)))
            .ReturnsAsync(new DeleteResult.Acknowledged(default));
        var target = new DocumentDao<TestDocument>(
            mockCollection.Object,
            new Mock<IValidator<TestDocument>>().Object);

        await target.Delete(Id, cancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task DocumentDao_Fetch()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
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

        var actual = await target.Fetch(d => d.Id == Id, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task DocumentDao_GetCount()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCollection = new Mock<IMongoCollection<TestDocument>>();
        var document = new TestDocument(Id);
        const int Expected = 1;
        _ = mockCollection.Setup(
            m => m.CountDocumentsAsync(
                It.IsAny<ExpressionFilterDefinition<TestDocument>>(),
                default,
                cancellationToken))
            .Callback<FilterDefinition<TestDocument>, CountOptions, CancellationToken>(
                (f, o, t) => TestUtilities.AssertExpressionFilter(f, document))
            .ReturnsAsync(Expected);
        var mockValidator = new Mock<IValidator<TestDocument>>();
        var target = new DocumentDao<TestDocument>(mockCollection.Object, mockValidator.Object);

        var actual = await target.GetCount(d => d.Id == Id, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(Expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(0, null, "Id", DocumentState.New)]
    [DataRow(1, "Id", null, DocumentState.Updated)]
    public async Task DocumentDao_Save(long matchedCount, string id, string upsertedId, DocumentState state)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var document = new TestDocument(id);
        var mockCollection = new Mock<IMongoCollection<TestDocument>>();
        _ = mockCollection.Setup(
            m => m.ReplaceOneAsync(
                It.IsAny<ExpressionFilterDefinition<TestDocument>>(),
                document,
                It.IsAny<ReplaceOptions>(),
                cancellationToken))
            .Callback<FilterDefinition<TestDocument>, TestDocument, ReplaceOptions, CancellationToken>(
                (f, d, o, t) =>
                {
                    TestUtilities.AssertExpressionFilter(f, document);
                    Assert.AreEqual(document, d);
                    Assert.IsTrue(o.IsUpsert);
                })
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(matchedCount, default, upsertedId));
        var target = new DocumentDao<TestDocument>(
            mockCollection.Object,
            new Mock<IValidator<TestDocument>>().Object);

        var result = await target.Save(document, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(state is DocumentState.New ? upsertedId : id, result.Id);
        Assert.AreEqual(state, result.State);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task DocumentDao_Save_DocumentIsInvalid_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var document = new TestDocument(Id);
        var validator = new InlineValidator<TestDocument>();
        _ = validator.RuleFor(d => d.Id).Must(_ => false);
        var target = new DocumentDao<TestDocument>(
            new Mock<IMongoCollection<TestDocument>>().Object,
            validator);

        _ = await target.Save(new TestDocument(Id), cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task DocumentDao_Update()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        const string Value = nameof(Value);
        var document = new TestDocument(Id) { Value = Value };
        var mockCollection = new Mock<IMongoCollection<TestDocument>>();
        _ = mockCollection.Setup(
            m => m.UpdateOneAsync(
                It.IsAny<ExpressionFilterDefinition<TestDocument>>(),
                It.IsAny<UpdateDefinition<TestDocument>>(),
                It.IsAny<UpdateOptions>(),
                cancellationToken))
            .Callback<FilterDefinition<TestDocument>, UpdateDefinition<TestDocument>, UpdateOptions, CancellationToken>(
                (f, u, o, t) =>
                {
                    TestUtilities.AssertExpressionFilter(f, document);
                    TestUtilities.AssertUpdate<TestDocument, string>(
                        f,
                        u,
                        document,
                        "value",
                        v => Assert.AreEqual(Value, v));
                })
            .ReturnsAsync(new UpdateResult.Acknowledged(default, default, default));
        var target = new DocumentDao<TestDocument>(
            mockCollection.Object,
            new Mock<IValidator<TestDocument>>().Object);

        await target.Update(Id, d => d.Value, Value, cancellationToken).ConfigureAwait(false);
    }
}
