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
using Hirameku.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.IdentityModel.Tokens.Jwt;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UserController : HiramekuController
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public UserController(
        IHttpContextAccessor contextAccessor,
        IValidator<ChangePasswordModel> changePasswordModelValidator,
        EmailAddressValidator emailAddressValidator,
        NameValidator nameValidator,
        UserNameValidator userNameValidator,
        IUserProvider userProvider)
        : base(contextAccessor)
    {
        this.ChangePasswordModelValidator = changePasswordModelValidator;
        this.EmailAddressValidator = emailAddressValidator;
        this.NameValidator = nameValidator;
        this.UserNameValidator = userNameValidator;
        this.UserProvider = userProvider;
    }

    private IValidator<ChangePasswordModel> ChangePasswordModelValidator { get; }

    private EmailAddressValidator EmailAddressValidator { get; }

    private NameValidator NameValidator { get; }

    private UserNameValidator UserNameValidator { get; }

    private IUserProvider UserProvider { get; }

    [HttpPost("changePassword")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordModel model,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { model, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var user = this.ContextAccessor.HttpContext?.User;
        IActionResult result;

        if (user?.Identity?.IsAuthenticated ?? false)
        {
            var validationResult = await this.ChangePasswordModelValidator.ValidateAsync(model, cancellationToken)
                .ConfigureAwait(false);

            if (validationResult.IsValid)
            {
                var responseModel = await this.UserProvider.ChangePassword(
                    new Authenticated<ChangePasswordModel>(
                        model,
                        await this.GetSecurityToken().ConfigureAwait(false),
                        user),
                    cancellationToken)
                    .ConfigureAwait(false);

                result = this.Ok(responseModel);
            }
            else
            {
                result = this.ValidationProblem(validationResult);
            }
        }
        else
        {
            result = this.Unauthorized();
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, result)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var user = this.ContextAccessor.HttpContext?.User;
        IActionResult result;

        if (user?.Identity?.IsAuthenticated ?? false)
        {
            await this.UserProvider.DeleteUser(
                new Authenticated<Unit>(
                    Unit.Value,
                    await this.GetSecurityToken().ConfigureAwait(false),
                    user),
                cancellationToken).ConfigureAwait(false);

            result = this.NoContent();
        }
        else
        {
            result = this.Unauthorized();
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, result)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var user = this.ContextAccessor.HttpContext?.User;
        IActionResult result;

        if (user?.Identity?.IsAuthenticated ?? false)
        {
            var userModel = await this.UserProvider.GetUser(
                new Authenticated<Unit>(
                    Unit.Value,
                    await this.GetSecurityToken().ConfigureAwait(false),
                    user),
                cancellationToken).ConfigureAwait(false);

            result = userModel != null ? this.Ok(userModel) : this.NotFound();
        }
        else
        {
            result = this.Unauthorized();
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, result)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }

    [HttpPatch("updateEmailAddress")]
    public async Task<IActionResult> UpdateEmailAddress(
        [FromBody] string emailAddress,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { emailAddress, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var user = this.ContextAccessor.HttpContext?.User;
        IActionResult result;

        if (user?.Identity?.IsAuthenticated ?? false)
        {
            var validationResult = await this.EmailAddressValidator.ValidateAsync(emailAddress, cancellationToken)
                .ConfigureAwait(false);

            if (validationResult.IsValid)
            {
                await this.UserProvider.UpdateEmailAddress(
                    new Authenticated<UpdateEmailAddressModel>(
                        new UpdateEmailAddressModel() { EmailAddress = emailAddress },
                        await this.GetSecurityToken().ConfigureAwait(false),
                        user),
                    cancellationToken)
                    .ConfigureAwait(false);

                result = this.NoContent();
            }
            else
            {
                result = this.ValidationProblem(validationResult);
            }
        }
        else
        {
            result = this.Unauthorized();
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, result)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }

    [HttpPatch("updateName")]
    public async Task<IActionResult> UpdateName(
        [FromBody] string name,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { name, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var user = this.ContextAccessor.HttpContext?.User;
        IActionResult result;

        if (user?.Identity?.IsAuthenticated ?? false)
        {
            var validationResult = await this.NameValidator.ValidateAsync(name, cancellationToken)
                .ConfigureAwait(false);

            if (validationResult.IsValid)
            {
                var sessionToken = await this.UserProvider.UpdateName(
                    new Authenticated<UpdateNameModel>(
                        new UpdateNameModel() { Name = name },
                        await this.GetSecurityToken().ConfigureAwait(false),
                        user),
                    cancellationToken)
                    .ConfigureAwait(false);

                result = this.Ok(sessionToken);
            }
            else
            {
                result = this.ValidationProblem(validationResult);
            }
        }
        else
        {
            result = this.Unauthorized();
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, result)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }

    [HttpPatch("updateUserName")]
    public async Task<IActionResult> UpdateUserName(
        [FromBody] string userName,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { userName, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var user = this.ContextAccessor.HttpContext?.User;
        IActionResult result;

        if (user?.Identity?.IsAuthenticated ?? false)
        {
            var validationResult = await this.UserNameValidator.ValidateAsync(userName, cancellationToken)
                .ConfigureAwait(false);

            if (validationResult.IsValid)
            {
                var sessionToken = await this.UserProvider.UpdateUserName(
                    new Authenticated<UpdateUserNameModel>(
                        new UpdateUserNameModel() { UserName = userName },
                        await this.GetSecurityToken().ConfigureAwait(false),
                        user),
                    cancellationToken)
                    .ConfigureAwait(false);

                result = this.Ok(sessionToken);
            }
            else
            {
                result = this.ValidationProblem(validationResult);
            }
        }
        else
        {
            result = this.Unauthorized();
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, result)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return result;
    }

    private async Task<JwtSecurityToken> GetSecurityToken()
    {
        var context = this.ContextAccessor.HttpContext;
        var token = context != null
            ? await context.GetTokenAsync("access_token").ConfigureAwait(false)
            : string.Empty;

        return new JwtSecurityToken(token);
    }
}
