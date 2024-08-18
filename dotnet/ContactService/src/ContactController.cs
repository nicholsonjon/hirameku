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

namespace Hirameku.ContactService;

using Asp.Versioning;
using AutoMapper;
using Hirameku.Common.Service;
using Hirameku.Contact;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ContactController : HiramekuController
{
    public ContactController(IMapper mapper)
        : base(mapper)
    {
    }

    [HttpPost("sendFeedback")]
    public Task<IActionResult> SendFeedback(
        [FromServices] ISendFeedbackHandler handler,
        [FromBody] SendFeedbackModel model,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            await handler.SendFeedback(
                model,
                nameof(this.SendFeedback),
                this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                cancellationToken)
                .ConfigureAwait(false);

            return this.Accepted();
        }

        return this.ExecuteAction(new { model, cancellationToken }, Action);
    }
}
