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

namespace Hirameku.Common.Service;

using FluentValidation;
using Hirameku.Common.Service.Properties;
using Microsoft.AspNetCore.Mvc;
using System.Net;

public static class ControllerBaseExtensions
{
    public static IActionResult ValidationProblem(this ControllerBase controller, ValidationException validationException)
    {
        ArgumentNullException.ThrowIfNull(validationException);

        var problem = new ValidationProblemDetails()
        {
            Detail = Resources.RequestValidationDetail,
            Instance = ErrorCodes.RequestValidationFailed,
            Status = (int)HttpStatusCode.BadRequest,
            Title = Resources.RequestValidationFailed,
        };

        foreach (var property in validationException.Errors.GroupBy(vf => vf.PropertyName))
        {
            problem.Errors.Add(property.Key, property.Select(vf => vf.ErrorMessage).ToArray());
        }

        var result = new BadRequestObjectResult(problem);
        result.ContentTypes.Add(MediaTypes.ProblemDetails);

        return result;
    }
}
