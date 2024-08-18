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
using AutoMapper;
using Hirameku.Common.Service;
using Hirameku.Registration;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class RegistrationController : HiramekuController
{
    public RegistrationController(IMapper mapper)
        : base(mapper)
    {
    }

    [HttpGet("isUserNameAvailable")]
    public Task<IActionResult> IsUserNameAvailable(
        [FromServices] IIsUserNameAvailableHandler handler,
        [FromQuery] string userName,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            var isUserNameAvailable = await handler.IsUserNameAvailable(userName, cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(isUserNameAvailable);
        }

        return this.ExecuteAction(new { userName, cancellationToken }, Action);
    }

    [HttpPost("register")]
    public Task<IActionResult> Register(
        [FromServices] IRegisterHandler handler,
        [FromBody] RegisterModel model,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            await handler.Register(
                model,
                nameof(this.Register),
                this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                cancellationToken)
                .ConfigureAwait(false);

            return this.Accepted();
        }

        return this.ExecuteAction(new { model, cancellationToken }, Action);
    }

    [HttpPost("rejectRegistration")]
    public Task<IActionResult> RejectRegistration(
        [FromServices] IRejectRegistrationHandler handler,
        [FromBody] string token,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            await handler.RejectRegistration(token, cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        return this.ExecuteAction(new { token, cancellationToken }, Action);
    }

    [HttpPost("resendVerificationEmail")]
    public Task<IActionResult> ResendVerificationEmail(
        [FromServices] IResendVerificationHandler handler,
        [FromBody] ResendVerificationEmailModel model,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            var result = await handler.ResendVerificationEmail(
                model,
                nameof(this.ResendVerificationEmail),
                this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(result);
        }

        return this.ExecuteAction(new { model, cancellationToken }, Action);
    }

    [HttpPost("validatePassword")]
    public Task<IActionResult> ValidatePassword(
        [FromServices] IValidatePasswordHandler handler,
        [FromBody] string password,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            var result = await handler.ValidatePassword(password, cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(result);
        }

        return this.ExecuteAction(new { password, cancellationToken }, Action);
    }

    [HttpPost("verifyEmailAddress")]
    public Task<IActionResult> VerifyEmailAddress(
        [FromServices] IVerifyEmailAddressHandler handler,
        [FromBody] string token,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            var result = await handler.VerifyEmaiAddress(token, cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(result);
        }

        return this.ExecuteAction(new { token, cancellationToken }, Action);
    }
}
