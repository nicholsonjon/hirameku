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

namespace Hirameku.IdentityService;

using FluentValidation;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Common.Service;
using Hirameku.Registration;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System;
using PasswordValidationResult = Hirameku.Registration.PasswordValidationResult;
using ServiceExceptions = Hirameku.Common.Service.Properties.Exceptions;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class RegistrationController : HiramekuController
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public RegistrationController(
        IHttpContextAccessor contextAccessor,
        Base64StringValidator base64StringValidator,
        IValidator<RegisterModel> registerModelValidator,
        IRegistrationProvider registrationProvider,
        IValidator<ResendVerificationEmailModel> resendVerificationEmailModelValidator,
        UserNameValidator userNameValidator)
        : base(contextAccessor)
    {
        this.Base64StringValidator = base64StringValidator;
        this.RegisterModelValidator = registerModelValidator;
        this.RegistrationProvider = registrationProvider;
        this.ResendVerificationEmailModelValidator = resendVerificationEmailModelValidator;
        this.UserNameValidator = userNameValidator;
    }

    private Base64StringValidator Base64StringValidator { get; }

    private IValidator<RegisterModel> RegisterModelValidator { get; }

    private IRegistrationProvider RegistrationProvider { get; }

    private IValidator<ResendVerificationEmailModel> ResendVerificationEmailModelValidator { get; }

    private UserNameValidator UserNameValidator { get; }

    [HttpGet("isUserNameAvailable")]
    public async Task<IActionResult> IsUserNameAvailable(
        [FromBody] string userName,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { userName, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var validationResult = await this.UserNameValidator.ValidateAsync(userName, cancellationToken)
            .ConfigureAwait(false);
        IActionResult result;

        if (validationResult.IsValid)
        {
            var isUserNameAvailable = await this.RegistrationProvider.IsUserNameAvailable(userName, cancellationToken)
                .ConfigureAwait(false);

            result = this.Ok(isUserNameAvailable);
        }
        else
        {
            result = this.ValidationProblem(validationResult);
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, result)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterModel model,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { model, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var validationResult = await this.RegisterModelValidator.ValidateAsync(model, cancellationToken)
            .ConfigureAwait(false);
        IActionResult result;

        if (validationResult.IsValid)
        {
            var context = this.ContextAccessor.HttpContext
                ?? throw new InvalidOperationException(ServiceExceptions.HttpContextIsNull);

            await this.RegistrationProvider.Register(
                model,
                nameof(this.Register),
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                cancellationToken)
                .ConfigureAwait(false);

            result = this.Accepted();
        }
        else
        {
            result = this.ValidationProblem(validationResult);
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, result)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }

    [HttpPost("rejectRegistration")]
    public async Task<IActionResult> RejectRegistration(
        [FromBody] string token,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { token = "REDACTED", cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var validationResult = await this.Base64StringValidator.ValidateAsync(token, cancellationToken)
            .ConfigureAwait(false);
        IActionResult result;

        if (validationResult.IsValid)
        {
            await this.RegistrationProvider.RejectRegistration(token, cancellationToken).ConfigureAwait(false);

            result = this.NoContent();
        }
        else
        {
            result = this.ValidationProblem(validationResult);
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, result)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }

    [HttpPost("resendVerificationEmail")]
    public async Task<IActionResult> ResendVerificationEmail(
        [FromBody] ResendVerificationEmailModel model,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { model, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var validationResult = await this.ResendVerificationEmailModelValidator.ValidateAsync(model, cancellationToken)
            .ConfigureAwait(false);
        IActionResult actionResult;

        if (validationResult.IsValid)
        {
            var context = this.ContextAccessor.HttpContext
                ?? throw new InvalidOperationException(ServiceExceptions.HttpContextIsNull);
            var result = await this.RegistrationProvider.ResendVerificationEmail(
                model,
                nameof(this.ResendVerificationEmail),
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                cancellationToken)
                .ConfigureAwait(false);

            actionResult = this.Ok(result);
        }
        else
        {
            actionResult = this.ValidationProblem(validationResult);
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, actionResult)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return actionResult;
    }

    [HttpPost("validatePassword")]
    public async Task<ActionResult<PasswordValidationResult>> ValidatePassword(
        [FromBody] string password,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { password = "REDACTED", cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var result = await this.RegistrationProvider.ValidatePassword(password, cancellationToken)
            .ConfigureAwait(false);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, result)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }

    [HttpPost("verifyEmailAddress")]
    public async Task<IActionResult> VerifyEmailAddress(
        [FromBody] string token,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { token = "REDACTED", cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var validationResult = await this.Base64StringValidator.ValidateAsync(token, cancellationToken)
            .ConfigureAwait(false);
        IActionResult actionResult;

        if (validationResult.IsValid)
        {
            var result = await this.RegistrationProvider.VerifyEmaiAddress(token, cancellationToken)
                .ConfigureAwait(false);

            actionResult = this.Ok(result);
        }
        else
        {
            actionResult = this.ValidationProblem(validationResult);
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, actionResult)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return actionResult;
    }
}
