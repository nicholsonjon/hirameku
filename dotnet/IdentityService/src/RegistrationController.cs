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

using Asp.Versioning;
using Hirameku.Common.Service;
using Hirameku.Registration;
using Microsoft.AspNetCore.Mvc;
using System;
using ServiceExceptions = Hirameku.Common.Service.Properties.Exceptions;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class RegistrationController : HiramekuController
{
    public RegistrationController(IHttpContextAccessor contextAccessor, IRegistrationProvider registrationProvider)
        : base(contextAccessor)
    {
        this.RegistrationProvider = registrationProvider;
    }

    private IRegistrationProvider RegistrationProvider { get; }

    [HttpGet("isUserNameAvailable")]
    public Task<IActionResult> IsUserNameAvailable(
        [FromQuery] string userName,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            var isUserNameAvailable = await this.RegistrationProvider.IsUserNameAvailable(userName, cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(isUserNameAvailable);
        }

        return this.ExecuteAction(new { userName, cancellationToken }, Action);
    }

    [HttpPost("register")]
    public Task<IActionResult> Register([FromBody] RegisterModel model, CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            var context = this.ContextAccessor.HttpContext
                ?? throw new InvalidOperationException(ServiceExceptions.HttpContextIsNull);

            await this.RegistrationProvider.Register(
                model,
                nameof(this.Register),
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                cancellationToken)
                .ConfigureAwait(false);

            return this.Accepted();
        }

        return this.ExecuteAction(new { model, cancellationToken }, Action);
    }

    [HttpPost("rejectRegistration")]
    public Task<IActionResult> RejectRegistration(
        [FromBody] string token,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            await this.RegistrationProvider.RejectRegistration(token, cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        return this.ExecuteAction(new { token, cancellationToken }, Action);
    }

    [HttpPost("resendVerificationEmail")]
    public Task<IActionResult> ResendVerificationEmail(
        [FromBody] ResendVerificationEmailModel model,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            var context = this.ContextAccessor.HttpContext
                ?? throw new InvalidOperationException(ServiceExceptions.HttpContextIsNull);
            var result = await this.RegistrationProvider.ResendVerificationEmail(
                model,
                nameof(this.ResendVerificationEmail),
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(result);
        }

        return this.ExecuteAction(new { model, cancellationToken }, Action);
    }

    [HttpPost("validatePassword")]
    public Task<IActionResult> ValidatePassword(
        [FromBody] string password,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            var result = await this.RegistrationProvider.ValidatePassword(password, cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(result);
        }

        return this.ExecuteAction(new { password, cancellationToken }, Action);
    }

    [HttpPost("verifyEmailAddress")]
    public Task<IActionResult> VerifyEmailAddress(
        [FromBody] string token,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            var result = await this.RegistrationProvider.VerifyEmaiAddress(token, cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(result);
        }

        return this.ExecuteAction(new { token, cancellationToken }, Action);
    }
}
