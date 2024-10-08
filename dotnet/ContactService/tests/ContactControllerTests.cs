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

namespace Hirameku.ContactService.Tests;

using AutoMapper;
using FluentValidation;
using Hirameku.Contact;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;

[TestClass]
public class ContactControllerTests
{
    private const string RemoteIP = "127.0.0.1";

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ContactController_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task ContactController_SendFeedback_Accepted()
    {
        var model = new SendFeedbackModel()
        {
            EmailAddress = nameof(SendFeedbackModel.EmailAddress),
            Feedback = nameof(SendFeedbackModel.Feedback),
            Name = nameof(SendFeedbackModel.Name),
            RecaptchaResponse = nameof(SendFeedbackModel.RecaptchaResponse),
        };
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockHandler = new Mock<ISendFeedbackHandler>();
        _ = mockHandler.Setup(
            m => m.SendFeedback(model, nameof(ContactController.SendFeedback), RemoteIP, cancellationToken))
            .Returns(Task.CompletedTask);
        var target = GetTarget();

        var result = await target.SendFeedback(mockHandler.Object, model, cancellationToken).ConfigureAwait(false);

        Assert.IsInstanceOfType(result, typeof(AcceptedResult));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task ContactController_SendFeedback_BadRequest()
    {
        var model = new SendFeedbackModel();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockHandler = new Mock<ISendFeedbackHandler>();
        _ = mockHandler.Setup(
            m => m.SendFeedback(model, nameof(ContactController.SendFeedback), RemoteIP, cancellationToken))
            .ThrowsAsync(new ValidationException(nameof(this.ContactController_SendFeedback_BadRequest)));
        var target = GetTarget();

        var result = await target.SendFeedback(mockHandler.Object, model, cancellationToken).ConfigureAwait(false);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.IsInstanceOfType(badRequest.Value, typeof(ValidationProblemDetails));
    }

    private static ContactController GetTarget()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(RemoteIP);

        return new ContactController(Mock.Of<IMapper>())
        {
            ControllerContext = new ControllerContext() { HttpContext = context },
        };
    }
}
