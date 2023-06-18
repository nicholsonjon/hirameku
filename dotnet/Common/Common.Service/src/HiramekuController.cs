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

using FluentValidation;
using Hirameku.Common.Properties;
using Hirameku.Common.Service.Properties;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
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

    protected virtual Task<IActionResult> AuthorizeAndExecuteAction<TParameters>(
        TParameters parameters,
        Func<ClaimsPrincipal, Task<IActionResult>> action)
    {
        async Task<IActionResult> Action()
        {
            var user = this.ContextAccessor.HttpContext?.User;
            IActionResult result;

            if (user?.Identity?.IsAuthenticated ?? false)
            {
                result = await action(user).ConfigureAwait(false);
            }
            else
            {
                result = this.Unauthorized();
            }

            return result;
        }

        return this.ExecuteAction(parameters, Action);
    }

    protected virtual async Task<IActionResult> ExecuteAction<TParameters>(
        TParameters parameters,
        Func<Task<IActionResult>> action)
    {
        Log.ForTraceEvent()
            .Message(LogMessages.EnteringMethod)
            .Property(LogProperties.Parameters, parameters)
            .Log();

        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        IActionResult result;

        try
        {
            result = await action().ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            result = this.ValidationProblem(ex);
        }

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Property(LogProperties.ReturnValue, result)
            .Log();

        return result;
    }

    protected async Task<JwtSecurityToken> GetSecurityToken()
    {
        var context = this.ContextAccessor.HttpContext;
        var token = context != null
            ? await context.GetTokenAsync("access_token").ConfigureAwait(false)
            : string.Empty;

        return new JwtSecurityToken(token);
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
}
