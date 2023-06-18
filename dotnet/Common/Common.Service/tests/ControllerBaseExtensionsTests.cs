namespace Hirameku.Common.Service.Tests;

using FluentValidation;
using FluentValidation.Results;
using Hirameku.Common.Service.Properties;
using Microsoft.AspNetCore.Mvc;
using System.Net;

[TestClass]
public class ControllerBaseExtensionsTests
{
    private const string ErrorMessage = nameof(ErrorMessage);
    private const string PropertyName = nameof(PropertyName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ControllerBaseExtensions_ValidationProblem()
    {
        var validationException = new ValidationException(
            new List<ValidationFailure>() { new ValidationFailure(PropertyName, ErrorMessage) });
        var result = ControllerBaseExtensions.ValidationProblem(null!, validationException) as BadRequestObjectResult;
        var problemDetails = result?.Value as ValidationProblemDetails;
        const int BadRequest = (int)HttpStatusCode.BadRequest;

        Assert.IsNotNull(problemDetails);
        Assert.IsTrue(result?.ContentTypes.Contains(MediaTypes.ProblemDetails) ?? false);
        Assert.AreEqual(BadRequest, result!.StatusCode);
        Assert.AreEqual(Resources.RequestValidationDetail, problemDetails.Detail);
        Assert.AreEqual(ErrorCodes.RequestValidationFailed, problemDetails.Instance);
        Assert.AreEqual(BadRequest, problemDetails.Status);
        Assert.AreEqual(Resources.RequestValidationFailed, problemDetails.Title);

        var error = problemDetails.Errors.Single();

        Assert.AreEqual(PropertyName, error.Key);
        Assert.AreEqual(ErrorMessage, error.Value.Single());
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ControllerBaseExtensions_ValidationProblem_ValidationExeception_Null_Throws()
    {
        _ = ControllerBaseExtensions.ValidationProblem(null!, null!);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }
}
