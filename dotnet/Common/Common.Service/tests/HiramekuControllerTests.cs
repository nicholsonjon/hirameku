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

namespace Hirameku.Common.Service.Tests;

using AutoMapper;
using FluentValidation.Results;
using Hirameku.Common.Service.Properties;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using System.Reflection;

[TestClass]
public class HiramekuControllerTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void HiramekuController_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task HiramekuController_Error()
    {
        var target = GetTarget(GetMockContextAccessor());

        var actionResult = await target.Error().ConfigureAwait(false);

        var result = actionResult as ObjectResult;
        var problemDetails = result?.Value as ProblemDetails;
        var statusCode = (int)HttpStatusCode.InternalServerError;
        Assert.IsTrue(result?.ContentTypes.Contains(MediaTypes.ProblemDetails) ?? false);
        Assert.AreEqual(result!.StatusCode, statusCode);
        Assert.IsNotNull(problemDetails);
        Assert.IsNull(problemDetails!.Detail);
        Assert.AreEqual(ErrorCodes.UnexpectedError, problemDetails.Instance);
        Assert.AreEqual(Resources.UnexpectedError, problemDetails.Title);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task HiramekuController_Error_ContextAccessorIsNull_Throws()
    {
        var mockAccessor = new Mock<IHttpContextAccessor>();
        _ = mockAccessor.Setup(m => m.HttpContext)
            .Returns(null as HttpContext);
        var target = GetTarget(mockAccessor);

        _ = await target.Error().ConfigureAwait(false);

        Assert.Fail(typeof(InvalidOperationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void HiramekuController_ValidationProblem()
    {
        var target = GetTarget();
        const string PropertyName = nameof(PropertyName);
        const string ErrorMessage = nameof(ErrorMessage);
        var validationResult = new ValidationResult(new ValidationFailure[]
        {
            new ValidationFailure(PropertyName, ErrorMessage),
        });

        var controllerType = typeof(TestHiramekuController);
        var methodInfo = controllerType.GetMethod(
            nameof(HiramekuController.ValidationProblem),
            BindingFlags.NonPublic | BindingFlags.Instance,
            new Type[] { typeof(ValidationResult) });

        Assert.IsNotNull(methodInfo);

        var result = methodInfo.Invoke(target, new object[] { validationResult }) as BadRequestObjectResult;
        var problemDetails = result?.Value as ValidationProblemDetails;
        const int BadRequest = (int)HttpStatusCode.BadRequest;

        Assert.IsNotNull(problemDetails);
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
    public void HiramekuController_ValidationProblem_ValidationResult_Null_Throws()
    {
        var target = GetTarget();

        _ = target.ValidationProblem((null as ValidationProblemDetails)!);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    private static Mock<IHttpContextAccessor> GetMockContextAccessor(Exception? error = default)
    {
        var exceptionHandlerFeature = new ExceptionHandlerFeature() { Error = error! };
        var mockFeatureCollection = new Mock<IFeatureCollection>();
        _ = mockFeatureCollection.Setup(m => m.Get<IExceptionHandlerFeature>())
            .Returns(exceptionHandlerFeature);
        var context = new DefaultHttpContext(mockFeatureCollection.Object);
        var mockAccessor = new Mock<IHttpContextAccessor>();
        _ = mockAccessor.Setup(m => m.HttpContext)
            .Returns(context);

        return mockAccessor;
    }

    private static Mock<IMapper> GetMockMapper(Exception exception, ProblemDetails problemDetails)
    {
        var mockMapper = new Mock<IMapper>();
        _ = mockMapper.Setup(m => m.Map<ProblemDetails>(exception))
            .Returns(problemDetails);

        return mockMapper;
    }

    private static HiramekuController GetTarget(Mock<IHttpContextAccessor>? mockContextAccessor = default)
    {
        return new TestHiramekuController(mockContextAccessor?.Object ?? Mock.Of<IHttpContextAccessor>());
    }
}
