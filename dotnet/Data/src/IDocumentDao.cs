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

using System.Linq.Expressions;

public interface IDocumentDao<TDocument>
    where TDocument : IDocument
{
    public Task Delete(string id, CancellationToken cancellationToken = default);

    public Task<TDocument?> Fetch(
        Expression<Func<TDocument, bool>> filter,
        CancellationToken cancellationToken = default);

    public Task<long> GetCount(
        Expression<Func<TDocument, bool>> filter,
        CancellationToken cancellationToken = default);

    public Task<SaveResult> Save(TDocument document, CancellationToken cancellationToken = default);

    public Task Update<TField>(
        string id,
        Expression<Func<TDocument, TField>> field,
        TField value,
        CancellationToken cancellationToken = default);
}
