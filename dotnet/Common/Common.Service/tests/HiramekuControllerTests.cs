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
using FluentValidation;
using Hirameku.Common.Service.Properties;
using Hirameku.TestTools;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using System.Net;
using System.Reflection;
using System.Security.Claims;

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
    public Task HiramekuController_AuthorizeAndExecuteAction()
    {
        var context = TestUtilities.GetControllerContext(TestUtilities.GetJwtSecurityToken(), TestUtilities.GetUser());

        Assert.IsNotNull(context.HttpContext);

        return RunAuthorizeAndExecuteActionTest<OkResult>(context);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public Task HiramekuController_AuthorizeAndExecuteAction_Unauthorized()
    {
        return RunAuthorizeAndExecuteActionTest<UnauthorizedResult>(
            TestUtilities.GetControllerContext(user: new ClaimsPrincipal(new ClaimsIdentity())));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task HiramekuController_Error()
    {
        const string Detail = nameof(Detail);
        const string Instance = nameof(Instance);
        const int Status = 999;
        const string Title = nameof(Title);
        var mockMapper = new Mock<IMapper>();
        var expectedException = new InvalidOperationException();
        var expectedProblemDetails = new ProblemDetails();
        mockMapper.Setup(m => m.Map<Exception, ProblemDetails>(expectedException, expectedProblemDetails))
            .Callback<Exception, ProblemDetails>(
                (_, d) =>
                {
                    d.Detail = Detail;
                    d.Instance = Instance;
                    d.Status = Status;
                    d.Title = Title;
                })
            .Returns(expectedProblemDetails)
            .Verifiable();
        var mockExceptionHandlerFeature = new Mock<IExceptionHandlerFeature>();
        _ = mockExceptionHandlerFeature.Setup(m => m.Error)
            .Returns(expectedException);
        var controllerContext = TestUtilities.GetControllerContext(
            mockExceptionHandlerFeature: mockExceptionHandlerFeature);
        var target = GetTarget(controllerContext, mockMapper);
        var mockProblemDetailsFactory = GetMockProblemDetailsFactory(
            controllerContext.HttpContext,
            expectedProblemDetails);
        target.ProblemDetailsFactory = mockProblemDetailsFactory.Object;

        var actionResult = await target.Error().ConfigureAwait(false);

        var result = actionResult as ObjectResult;
        var problemDetails = result?.Value as ProblemDetails;

        mockProblemDetailsFactory.Verify();
        mockMapper.Verify();
        Assert.IsTrue(result?.ContentTypes.Contains(MediaTypes.ProblemDetails) ?? false);
        Assert.AreEqual(Status, result?.StatusCode);
        Assert.IsNotNull(problemDetails);
        Assert.AreEqual(Detail, problemDetails!.Detail);
        Assert.AreEqual(Instance, problemDetails.Instance);
        Assert.AreEqual(Title, problemDetails.Title);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task HiramekuController_ExecuteAction()
    {
        var target = GetTarget();
        var controllerType = typeof(TestHiramekuController);
        var methodInfo = controllerType.GetMethod("ExecuteAction", BindingFlags.NonPublic | BindingFlags.Instance);
        var genericMethod = methodInfo?.MakeGenericMethod(typeof(object));

        Assert.IsNotNull(genericMethod);

        Func<Task<IActionResult>> action = () => Task.FromResult(new OkResult() as IActionResult);
        var task = genericMethod.Invoke(target, new object[] { new(), action }) as Task<IActionResult>;

        Assert.IsNotNull(task);

        var result = await task.ConfigureAwait(false);

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<OkResult>(result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task HiramekuController_ExecuteAction_ActionIsNull_Throws()
    {
        var target = GetTarget();
        var controllerType = typeof(TestHiramekuController);
        var methodInfo = controllerType.GetMethod("ExecuteAction", BindingFlags.NonPublic | BindingFlags.Instance);
        var genericMethod = methodInfo?.MakeGenericMethod(typeof(object));

        Assert.IsNotNull(genericMethod);

        try
        {
            var task = genericMethod.Invoke(target, new object[] { new(), null! }) as Task;

            await task!.ConfigureAwait(false);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task HiramekuController_ExecuteAction_ValidationException()
    {
        var target = GetTarget();
        var controllerType = typeof(TestHiramekuController);
        var methodInfo = controllerType.GetMethod("ExecuteAction", BindingFlags.NonPublic | BindingFlags.Instance);
        var genericMethod = methodInfo?.MakeGenericMethod(typeof(object));

        Assert.IsNotNull(genericMethod);

        Func<Task<IActionResult>> action = () => throw new ValidationException("error");

        var task = genericMethod.Invoke(target, new object[] { new(), action }) as Task<IActionResult>;

        Assert.IsNotNull(task);

        var result = await task.ConfigureAwait(false);

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task HiramekuController_GetSecurityToken()
    {
        const string UserName = nameof(UserName);
        const string UserId = nameof(UserId);
        const string Name = nameof(Name);
        var expected = TestUtilities.GetJwtSecurityToken(UserName, UserId, Name);
        var target = GetTarget(TestUtilities.GetControllerContext(expected));

        var actual = await target.GetSecurityToken().ConfigureAwait(false);

        Assert.AreEqual(expected.RawData, actual.RawData);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void HiramekuController_Problem()
    {
        const string Detail = nameof(Detail);
        const string Instance = nameof(Instance);
        const int Status = 999;
        const string Title = nameof(Title);
        var mockMapper = new Mock<IMapper>();
        var expectedException = new InvalidOperationException();
        var expectedProblemDetails = new ProblemDetails();
        mockMapper.Setup(m => m.Map<Exception, ProblemDetails>(expectedException, expectedProblemDetails))
            .Callback<Exception, ProblemDetails>(
                (_, d) =>
                {
                    d.Detail = Detail;
                    d.Instance = Instance;
                    d.Status = Status;
                    d.Title = Title;
                })
            .Returns(expectedProblemDetails)
            .Verifiable();
        var controllerContext = TestUtilities.GetControllerContext();
        var target = GetTarget(controllerContext, mockMapper);
        var mockProblemDetailsFactory = GetMockProblemDetailsFactory(
            controllerContext.HttpContext,
            expectedProblemDetails);
        target.ProblemDetailsFactory = mockProblemDetailsFactory.Object;
        var controllerType = typeof(TestHiramekuController);
        var methodInfo = controllerType.GetMethod(
            nameof(TestHiramekuController.Problem),
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(Exception)]);

        Assert.IsNotNull(methodInfo);

        var result = methodInfo.Invoke(target, new object[] { expectedException }) as ObjectResult;
        var actualProblemDetails = result?.Value as ProblemDetails;

        mockProblemDetailsFactory.Verify();
        mockMapper.Verify();
        Assert.IsNotNull(actualProblemDetails);
        Assert.AreEqual(Detail, actualProblemDetails.Detail);
        Assert.AreEqual(Instance, actualProblemDetails.Instance);
        Assert.AreEqual(Status, actualProblemDetails.Status);
        Assert.AreEqual(Title, actualProblemDetails.Title);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void HiramekuController_Problem_AutoMapperMappingException()
    {
        var detail = Resources.UnexpectedError;
        var instance = ErrorCodes.UnexpectedError;
        var status = (int)HttpStatusCode.InternalServerError;
        var title = Resources.UnexpectedError;
        var mockMapper = new Mock<IMapper>();
        var expectedProblemDetails = new ProblemDetails();
        var expectedException = new InvalidOperationException();
        _ = mockMapper.Setup(m => m.Map<Exception, ProblemDetails>(expectedException, expectedProblemDetails))
            .Throws(new AutoMapperMappingException());
        var controllerContext = TestUtilities.GetControllerContext();
        var target = GetTarget(controllerContext, mockMapper);
        var mockProblemDetailsFactory = GetMockProblemDetailsFactory(
            controllerContext.HttpContext,
            expectedProblemDetails,
            detail,
            title,
            status,
            instance);
        target.ProblemDetailsFactory = mockProblemDetailsFactory.Object;
        var controllerType = typeof(TestHiramekuController);
        var methodInfo = controllerType.GetMethod(
            nameof(TestHiramekuController.Problem),
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(Exception)]);

        Assert.IsNotNull(methodInfo);

        var result = methodInfo.Invoke(target, new object[] { expectedException }) as ObjectResult;
        var actualProblemDetails = result?.Value as ProblemDetails;

        Assert.IsNotNull(actualProblemDetails);
        Assert.AreEqual(detail, actualProblemDetails.Detail);
        Assert.AreEqual(instance, actualProblemDetails.Instance);
        Assert.AreEqual(status, actualProblemDetails.Status);
        Assert.AreEqual(title, actualProblemDetails.Title);
    }

    private static ControllerContext GetControllerContext(Exception? error = default)
    {
        var exceptionHandlerFeature = new ExceptionHandlerFeature() { Error = error! };
        var mockFeatureCollection = new Mock<IFeatureCollection>();
        _ = mockFeatureCollection.Setup(m => m.Get<IExceptionHandlerFeature>())
            .Returns(exceptionHandlerFeature);

        return new ControllerContext()
        {
            HttpContext = new DefaultHttpContext(mockFeatureCollection.Object),
        };
    }

    private static Mock<ProblemDetailsFactory> GetMockProblemDetailsFactory(
        HttpContext expectedContext,
        ProblemDetails problemDetails,
        string? detail = default,
        string? title = default,
        int? status = 500,
        string? instance = default)
    {
        var mockProblemDetailsFactory = new Mock<ProblemDetailsFactory>();
        mockProblemDetailsFactory
            .Setup(m => m.CreateProblemDetails(
                expectedContext,
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(problemDetails ?? new ProblemDetails())
            .Verifiable();
        _ = mockProblemDetailsFactory
            .Setup(m => m.CreateProblemDetails(
                expectedContext,
                status,
                title,
                default,
                detail,
                instance))
            .Returns(new ProblemDetails()
            {
                Detail = detail,
                Instance = instance,
                Status = status,
                Title = title,
            });

        return mockProblemDetailsFactory;
    }

    private static TestHiramekuController GetTarget(
        ControllerContext? context = default,
        Mock<IMapper>? mockMapper = default)
    {
        return new TestHiramekuController(mockMapper?.Object ?? Mock.Of<IMapper>())
        {
            ControllerContext = context ?? new ControllerContext(),
        };
    }

    private static async Task RunAuthorizeAndExecuteActionTest<TResult>(ControllerContext context)
        where TResult : class, IActionResult
    {
        var target = GetTarget(context);

        Assert.IsNotNull(target.ControllerContext.HttpContext);

        var controllerType = typeof(TestHiramekuController);
        var methodInfo = controllerType.GetMethod(
            "AuthorizeAndExecuteAction",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var genericMethod = methodInfo?.MakeGenericMethod(typeof(object));

        Assert.IsNotNull(genericMethod);

        Func<ClaimsPrincipal, Task<IActionResult>> action = _ => Task.FromResult(new OkResult() as IActionResult);
        var task = genericMethod.Invoke(target, [new(), action]) as Task<IActionResult>;

        Assert.IsNotNull(task);

        var result = await task.ConfigureAwait(false);

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<TResult>(result);
    }
}
