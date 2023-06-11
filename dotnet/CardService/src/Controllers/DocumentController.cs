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

namespace Hirameku.CardService.Controllers;

using AutoMapper;
using Hirameku.Common;
using Hirameku.Data;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Diagnostics;

public abstract class DocumentController<TModel, TDocument> : ControllerBase
    where TModel : class
    where TDocument : IDocument
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    protected DocumentController(IDocumentDao<TDocument> dao, IGuidProvider guidProvider, IMapper mapper)
    {
        this.Dao = dao;
        this.GuidProvider = guidProvider;
        this.Mapper = mapper;
    }

    protected IDocumentDao<TDocument> Dao { get; }

    protected IGuidProvider GuidProvider { get; }

    protected IMapper Mapper { get; }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        Trace.CorrelationManager.ActivityId = this.GuidProvider.GenerateGuid();

        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new { id, cancellationToken },
                rawUrl = this.ControllerContext.HttpContext.Request.GetDisplayUrl(),
                user = this.User.Identity?.Name,
            });

        await this.Dao.Delete(id, cancellationToken).ConfigureAwait(false);

        var result = this.NoContent();

        Log.Trace("Exiting method", data: new { returnValue = result });

        return result;
    }

    [HttpGet("{id}")]
    public async Task<TModel> Get(string id, CancellationToken cancellationToken)
    {
        Trace.CorrelationManager.ActivityId = this.GuidProvider.GenerateGuid();

        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new { id, cancellationToken },
                rawUrl = this.ControllerContext.HttpContext.Request.GetDisplayUrl(),
                user = this.User.Identity?.Name,
            });

        var document = await this.Get(id, cancellationToken).ConfigureAwait(false);
        var result = this.Mapper.Map<TModel>(document);

        Log.Trace("Exiting method", data: new { returnValue = result });

        return result;
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] TModel model, CancellationToken cancellationToken)
    {
        Trace.CorrelationManager.ActivityId = this.GuidProvider.GenerateGuid();

        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new { model, cancellationToken },
                rawUrl = this.ControllerContext.HttpContext.Request.GetDisplayUrl(),
                user = this.User.Identity?.Name,
            });

        var document = this.Mapper.Map<TDocument>(model);

        _ = await this.Dao.Save(document, cancellationToken).ConfigureAwait(false);

        var result = this.NoContent();

        Log.Trace("Exiting method", data: new { returnValue = result });

        return result;
    }
}
