﻿// Hirameku is a cloud-native, vendor-agnostic, serverless application for
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
using Hirameku.Authentication;
using Hirameku.Common;
using Hirameku.Common.Service;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthenticationController : HiramekuController
{
    public AuthenticationController(IMapper mapper)
        : base(mapper)
    {
    }

    [HttpPost("renewToken")]
    public Task<IActionResult> RenewToken(
        [FromServices] IRenewTokenHandler handler,
        [FromBody] RenewTokenModel model,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            var context = this.HttpContext;
            var headers = context.Request.Headers;
            var data = new AuthenticationData<RenewTokenModel>(
                headers.Accept.FirstOrDefault() ?? string.Empty,
                headers.ContentEncoding.FirstOrDefault() ?? string.Empty,
                headers.ContentLanguage.FirstOrDefault() ?? string.Empty,
                model,
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                headers.UserAgent.FirstOrDefault() ?? string.Empty);
            var renewTokenResult = await handler.RenewToken(data, cancellationToken)
                .ConfigureAwait(false);
            var authenticationResult = renewTokenResult.AuthenticationResult;

            return authenticationResult is AuthenticationResult.Authenticated
                ? this.Ok(renewTokenResult.SessionToken)
                : this.Unauthorized(authenticationResult);
        }

        return this.ExecuteAction(new { model, cancellationToken }, Action);
    }

    [HttpPost("resetPassword")]
    public Task<IActionResult> ResetPassword(
        [FromServices] IResetPasswordHandler handler,
        [FromBody] ResetPasswordModel model,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            var result = await handler.ResetPassword(
                model,
                nameof(this.ResetPassword),
                this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(result);
        }

        return this.ExecuteAction(new { model, cancellationToken }, Action);
    }

    [HttpPost("sendPasswordReset")]
    public Task<IActionResult> SendPasswordReset(
        [FromServices] ISendPasswordResetHandler handler,
        [FromBody] SendPasswordResetModel model,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            await handler.SendPasswordReset(
                model,
                nameof(this.SendPasswordReset),
                this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                cancellationToken)
                .ConfigureAwait(false);

            return this.Accepted();
        }

        return this.ExecuteAction(new { model, cancellationToken }, Action);
    }

    [HttpPost("signIn")]
    public Task<IActionResult> SignIn(
        [FromServices] ISignInHandler handler,
        [FromBody] SignInModel model,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            var context = this.HttpContext;
            var headers = context.Request.Headers;
            var data = new AuthenticationData<SignInModel>(
                headers.Accept.FirstOrDefault() ?? string.Empty,
                headers.ContentEncoding.FirstOrDefault() ?? string.Empty,
                headers.ContentLanguage.FirstOrDefault() ?? string.Empty,
                model,
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                headers.UserAgent.FirstOrDefault() ?? string.Empty);
            var signInResult = await handler.SignIn(data, cancellationToken).ConfigureAwait(false);
            var authenticationResult = signInResult.AuthenticationResult;

            return authenticationResult is AuthenticationResult.Authenticated or AuthenticationResult.PasswordExpired
                ? this.Ok(this.Mapper.Map<TokenResponseModel>(signInResult))
                : this.Unauthorized(signInResult.AuthenticationResult);
        }

        return this.ExecuteAction(new { model, cancellationToken }, Action);
    }
}
