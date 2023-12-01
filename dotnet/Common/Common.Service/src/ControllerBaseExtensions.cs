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
