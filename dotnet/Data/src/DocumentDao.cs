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

namespace Hirameku.Data;

using FluentValidation;
using Hirameku.Common;
using MongoDB.Driver;
using NLog;
using System.Linq.Expressions;
using System.Threading.Tasks;

public class DocumentDao<TDocument> : IDocumentDao<TDocument>
    where TDocument : class, IDocument
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public DocumentDao(IMongoCollection<TDocument> collection, IValidator<TDocument> validator)
    {
        this.Collection = collection;
        this.Validator = validator;
    }

    private IMongoCollection<TDocument> Collection { get; }

    private IValidator<TDocument> Validator { get; }

    public async Task Delete(string id, CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { id, cancellationToken });

        var result = await this.Collection.DeleteOneAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
        var deletedCount = result.DeletedCount;

        Log.Debug("Delete result", data: new { deletedCount });

        if (deletedCount == 0)
        {
            Log.Warn("Document was not deleted. Did another thread already delete it?", data: new { id });
        }

        Log.Trace("Exiting method", data: default(object));
    }

    public async Task<TDocument?> Fetch(
        Expression<Func<TDocument, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { filter, cancellationToken });

        using var cursor = await this.Collection.FindAsync(filter, default, cancellationToken)
            .ConfigureAwait(false);
        var document = await cursor.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        Log.Trace("Exiting method", data: new { returnValue = document });

        return document;
    }

    public async Task<long> GetCount(
        Expression<Func<TDocument, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { filter, cancellationToken });

        var count = await this.Collection.CountDocumentsAsync(
            filter,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        Log.Trace("Exiting method", data: new { returnValue = count });

        return count;
    }

    public async Task<SaveResult> Save(TDocument document, CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { document, cancellationToken });

        ArgumentNullException.ThrowIfNull(document);

        await this.Validator.ValidateAndThrowAsync(document, cancellationToken).ConfigureAwait(false);

        var replaceResult = await this.Collection.ReplaceOneAsync(
            d => d.Id == document.Id,
            document,
            new ReplaceOptions() { IsUpsert = true },
            cancellationToken)
            .ConfigureAwait(false);

        var saveResult = new SaveResult()
        {
            Id = document.Id ?? replaceResult.UpsertedId.AsString,
            State = replaceResult.MatchedCount == 0 ? DocumentState.New : DocumentState.Updated,
        };

        Log.Debug("Save result", data: saveResult);
        Log.Trace("Exiting method", data: new { returnValue = saveResult });

        return saveResult;
    }

    public async Task Update<TField>(
        string id,
        Expression<Func<TDocument, TField>> field,
        TField value,
        CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { id, field, value, cancellationToken });

        var result = await this.Collection.UpdateOneAsync(
            d => d.Id == id,
            Builders<TDocument>.Update.Set(field, value),
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var matchedCount = result.MatchedCount;
        var modifiedCount = result.IsModifiedCountAvailable ? result.ModifiedCount : 0;

        Log.Debug("Update result", data: new { id, matchedCount, modifiedCount });

        if (matchedCount == 0 || modifiedCount == 0)
        {
            Log.Warn(
                "Document was not found or was not modified. Did another thread modify or delete it?",
                data: new { id });
        }

        Log.Trace("Exiting method", data: default(object));
    }
}
