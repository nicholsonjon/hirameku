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

using Hirameku.Common.Service;
using Hirameku.Contact;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceExceptions = Hirameku.Common.Service.Properties.Exceptions;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ContactController : HiramekuController
{
    public ContactController(IHttpContextAccessor contextAccessor, IContactProvider provider)
        : base(contextAccessor)
    {
        this.Provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    private IContactProvider Provider { get; }

    [HttpPost("sendFeedback")]
    public Task<IActionResult> SendFeedback(
        [FromBody] SendFeedbackModel model,
        CancellationToken cancellationToken = default)
    {
        async Task<IActionResult> Action()
        {
            var context = this.ContextAccessor.HttpContext
                ?? throw new InvalidOperationException(ServiceExceptions.HttpContextIsNull);

            await this.Provider.SendFeedback(
                model,
                nameof(this.SendFeedback),
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                cancellationToken)
                .ConfigureAwait(false);

            return this.Accepted();
        }

        return this.ExecuteAction(new { model, cancellationToken }, Action);
    }
}
