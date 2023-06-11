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

using AutoMapper;
using FluentValidation;
using Hirameku.Authentication;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Common.Service;
using Microsoft.AspNetCore.Mvc;
using NLog;
using ServiceExceptions = Hirameku.Common.Service.Properties.Exceptions;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthenticationController : HiramekuController
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public AuthenticationController(
        IHttpContextAccessor contextAccessor,
        IAuthenticationProvider authenticationProvider,
        IMapper mapper,
        IValidator<RenewTokenModel> renewTokenModelValidator,
        IValidator<ResetPasswordModel> resetPasswordModelValidator,
        IValidator<SendPasswordResetModel> sendPasswordResetModelValidator,
        IValidator<SignInModel> signInModelValidator)
        : base(contextAccessor)
    {
        this.AuthenticationProvider = authenticationProvider;
        this.Mapper = mapper;
        this.RenewTokenModelValidator = renewTokenModelValidator;
        this.ResetPasswordModelValidator = resetPasswordModelValidator;
        this.SendPasswordResetModelValidator = sendPasswordResetModelValidator;
        this.SignInModelValidator = signInModelValidator;
    }

    private IAuthenticationProvider AuthenticationProvider { get; }

    private IMapper Mapper { get; }

    private IValidator<RenewTokenModel> RenewTokenModelValidator { get; }

    private IValidator<ResetPasswordModel> ResetPasswordModelValidator { get; }

    private IValidator<SendPasswordResetModel> SendPasswordResetModelValidator { get; }

    private IValidator<SignInModel> SignInModelValidator { get; }

    [HttpPost("renewToken")]
    public async Task<IActionResult> RenewToken(
        [FromBody] RenewTokenModel model,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Message(LogMessages.EnteringMethod)
            .Property(LogProperties.Parameters, new { model, cancellationToken })
            .Log();

        var validationResult = await this.RenewTokenModelValidator.ValidateAsync(model, cancellationToken)
            .ConfigureAwait(false);
        IActionResult actionResult;

        if (validationResult.IsValid)
        {
            var context = this.ContextAccessor.HttpContext
                ?? throw new InvalidOperationException(ServiceExceptions.HttpContextIsNull);
            var headers = context.Request.Headers;
            var data = new AuthenticationData<RenewTokenModel>(
                headers.Accept.FirstOrDefault() ?? string.Empty,
                headers.ContentEncoding.FirstOrDefault() ?? string.Empty,
                headers.ContentLanguage.FirstOrDefault() ?? string.Empty,
                model,
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                headers.UserAgent.FirstOrDefault() ?? string.Empty);
            var renewTokenResult = await this.AuthenticationProvider.RenewToken(data, cancellationToken)
                .ConfigureAwait(false);
            var authenticationResult = renewTokenResult.AuthenticationResult;

            if (authenticationResult is AuthenticationResult.Authenticated)
            {
                actionResult = this.Ok(renewTokenResult.SessionToken);
            }
            else
            {
                actionResult = this.Unauthorized(authenticationResult);
            }
        }
        else
        {
            actionResult = this.ValidationProblem(validationResult);
        }

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Property(LogProperties.ReturnValue, actionResult)
            .Log();

        return actionResult;
    }

    [HttpPost("resetPassword")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordModel model,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Message(LogMessages.EnteringMethod)
            .Property(LogProperties.Parameters, new { model, cancellationToken })
            .Log();

        var validationResult = await this.ResetPasswordModelValidator.ValidateAsync(model, cancellationToken)
            .ConfigureAwait(false);
        IActionResult actionResult;

        if (validationResult.IsValid)
        {
            var context = this.ContextAccessor.HttpContext
                ?? throw new InvalidOperationException(ServiceExceptions.HttpContextIsNull);
            var result = await this.AuthenticationProvider.ResetPassword(
                model,
                context.Request.Host.Host,
                nameof(this.ResetPassword),
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
            .Message(LogMessages.ExitingMethod)
            .Property(LogProperties.ReturnValue, actionResult)
            .Log();

        return actionResult;
    }

    [HttpPost("sendPasswordReset")]
    public async Task<IActionResult> SendPasswordReset(
        [FromBody] SendPasswordResetModel model,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Message(LogMessages.EnteringMethod)
            .Property(LogProperties.Parameters, new { model, cancellationToken })
            .Log();

        var validationResult = await this.SendPasswordResetModelValidator.ValidateAsync(model, cancellationToken)
            .ConfigureAwait(false);
        IActionResult actionResult;

        if (validationResult.IsValid)
        {
            var context = this.ContextAccessor.HttpContext
                ?? throw new InvalidOperationException(ServiceExceptions.HttpContextIsNull);
            await this.AuthenticationProvider.SendPasswordReset(
                model,
                context.Request.Host.Host,
                nameof(this.SendPasswordReset),
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                cancellationToken)
                .ConfigureAwait(false);

            actionResult = this.Accepted();
        }
        else
        {
            actionResult = this.ValidationProblem(validationResult);
        }

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Property(LogProperties.ReturnValue, actionResult)
            .Log();

        return actionResult;
    }

    [HttpPost("signIn")]
    public async Task<IActionResult> SignIn(
        [FromBody] SignInModel model,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Message(LogMessages.EnteringMethod)
            .Property(LogProperties.Parameters, new { model, cancellationToken })
            .Log();

        var validationResult = await this.SignInModelValidator.ValidateAsync(model, cancellationToken)
            .ConfigureAwait(false);
        IActionResult actionResult;

        if (validationResult.IsValid)
        {
            var context = this.ContextAccessor.HttpContext
                ?? throw new InvalidOperationException(ServiceExceptions.HttpContextIsNull);
            var headers = context.Request.Headers;
            var data = new AuthenticationData<SignInModel>(
                headers.Accept.FirstOrDefault() ?? string.Empty,
                headers.ContentEncoding.FirstOrDefault() ?? string.Empty,
                headers.ContentLanguage.FirstOrDefault() ?? string.Empty,
                model,
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                headers.UserAgent.FirstOrDefault() ?? string.Empty);
            var signInResult = await this.AuthenticationProvider.SignIn(data, cancellationToken).ConfigureAwait(false);
            var authenticationResult = signInResult.AuthenticationResult;

            if (authenticationResult is AuthenticationResult.Authenticated or AuthenticationResult.PasswordExpired)
            {
                actionResult = this.Ok(this.Mapper.Map<TokenResponseModel>(signInResult));
            }
            else
            {
                actionResult = this.Unauthorized(signInResult.AuthenticationResult);
            }
        }
        else
        {
            actionResult = this.ValidationProblem(validationResult);
        }

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Property(LogProperties.ReturnValue, actionResult)
            .Log();

        return actionResult;
    }
}
