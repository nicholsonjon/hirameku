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

namespace Hirameku.Common.Service;

using FluentValidation.Results;
using Hirameku.Common.Properties;
using Hirameku.Common.Service.Properties;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Net;
using ServiceExceptions = Hirameku.Common.Service.Properties.Exceptions;

public abstract class HiramekuController : ControllerBase
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    protected HiramekuController(IHttpContextAccessor contextAccessor)
    {
        this.ContextAccessor = contextAccessor;
    }

    protected IHttpContextAccessor ContextAccessor { get; }

    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("/error")]
    public Task<IActionResult> Error()
    {
        Log.ForTraceEvent()
            .Message(LogMessages.EnteringMethod)
            .Log();

        var context = this.ContextAccessor.HttpContext
            ?? throw new InvalidOperationException(ServiceExceptions.HttpContextIsNull);
        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandlerFeature?.Error;

        Log.ForErrorEvent()
            .Message("Unhandled exception occurred")
            .Exception(exception)
            .Log();

        var result = this.Problem(exception);
        result.ContentTypes.Add(MediaTypes.ProblemDetails);

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Property(LogProperties.ReturnValue, result)
            .Log();

        return Task.FromResult((IActionResult)result);
    }

    protected virtual ObjectResult Problem(Exception? exception)
    {
        Log.ForTraceEvent()
            .Message(LogMessages.EnteringMethod)
            .Property(LogProperties.Parameters, new { exception })
            .Log();

        Log.ForInfoEvent()
            .Message("Generating ProblemDetails for exception")
            .Exception(exception)
            .Log();

        var result = this.Problem(
            instance: ErrorCodes.UnexpectedError,
            statusCode: (int)HttpStatusCode.InternalServerError,
            title: Resources.UnexpectedError);

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Property(LogProperties.ReturnValue, result)
            .Log();

        return result;
    }

    protected IActionResult ValidationProblem(ValidationResult validationResult)
    {
        if (validationResult == null)
        {
            throw new ArgumentNullException(nameof(validationResult));
        }

        var problem = new ValidationProblemDetails()
        {
            Detail = Resources.RequestValidationDetail,
            Instance = ErrorCodes.RequestValidationFailed,
            Status = (int)HttpStatusCode.BadRequest,
            Title = Resources.RequestValidationFailed,
        };

        foreach (var property in validationResult.Errors.GroupBy(vf => vf.PropertyName))
        {
            problem.Errors.Add(property.Key, property.Select(vf => vf.ErrorMessage).ToArray());
        }

        return this.ValidationProblem(problem);
    }
}
