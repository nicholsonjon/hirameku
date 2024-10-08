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
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.User;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UserController : HiramekuController
{
    public UserController(IMapper mapper)
        : base(mapper)
    {
    }

    [HttpPost("changePassword")]
    public Task<IActionResult> ChangePassword(
        [FromServices] IChangePasswordHandler handler,
        [FromBody] ChangePasswordModel model,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action(ClaimsPrincipal user)
        {
            var responseModel = await handler.ChangePassword(
                new Authenticated<ChangePasswordModel>(model, user),
                cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(responseModel);
        }

        return this.AuthorizeAndExecuteAction(new { model, cancellationToken }, Action);
    }

    [HttpDelete]
    public Task<IActionResult> Delete(
        [FromServices] IDeleteUserHandler handler,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action(ClaimsPrincipal user)
        {
            await handler.DeleteUser(
                new Authenticated<Unit>(Unit.Value, user),
                cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        return this.AuthorizeAndExecuteAction(new { cancellationToken }, Action);
    }

    [HttpGet]
    public Task<IActionResult> Get(
        [FromServices] IGetUserHandler handler,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action(ClaimsPrincipal user)
        {
            var userModel = await handler.GetUser(
                new Authenticated<Unit>(Unit.Value, user),
                cancellationToken).ConfigureAwait(false);

            return userModel != null ? this.Ok(userModel) : this.NotFound();
        }

        return this.AuthorizeAndExecuteAction(new { cancellationToken }, Action);
    }

    [HttpPatch("updateEmailAddress")]
    public Task<IActionResult> UpdateEmailAddress(
        [FromServices] IUpdateEmailAddressHandler handler,
        [FromBody] string emailAddress,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action(ClaimsPrincipal user)
        {
            await handler.UpdateEmailAddress(
                new Authenticated<UpdateEmailAddressModel>(
                    new UpdateEmailAddressModel() { EmailAddress = emailAddress },
                    user),
                cancellationToken)
                .ConfigureAwait(false);

            return this.NoContent();
        }

        return this.AuthorizeAndExecuteAction(new { emailAddress, cancellationToken }, Action);
    }

    [HttpPatch("updateName")]
    public Task<IActionResult> UpdateName(
        [FromServices] IUpdateNameHandler handler,
        [FromBody] string name,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action(ClaimsPrincipal user)
        {
            var sessionToken = await handler.UpdateName(
                new Authenticated<UpdateNameModel>(new UpdateNameModel() { Name = name }, user),
                cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(sessionToken);
        }

        return this.AuthorizeAndExecuteAction(new { name, cancellationToken }, Action);
    }

    [HttpPatch("updateUserName")]
    public Task<IActionResult> UpdateUserName(
        [FromServices] IUpdateUserNameHandler handler,
        [FromBody] string userName,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action(ClaimsPrincipal user)
        {
            var sessionToken = await handler.UpdateUserName(
                new Authenticated<UpdateUserNameModel>(new UpdateUserNameModel() { UserName = userName }, user),
                cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(sessionToken);
        }

        return this.AuthorizeAndExecuteAction(new { userName, cancellationToken }, Action);
    }
}
