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

using Hirameku.Common.Service.Properties;
using Hirameku.TestTools;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.IdentityModel.Tokens.Jwt;
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
        return RunAuthorizeAndExecuteActionTest<OkResult>(
            TestUtilities.GetMockContextAccessor(TestUtilities.GetJwtSecurityToken(), TestUtilities.GetUser()));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public Task HiramekuController_AuthorizeAndExecuteAction_Unauthorized()
    {
        return RunAuthorizeAndExecuteActionTest<UnauthorizedResult>();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task HiramekuController_Error()
    {
        var target = GetTarget(GetMockContextAccessor());

        var actionResult = await target.Error().ConfigureAwait(false);

        var result = actionResult as ObjectResult;
        var problemDetails = result?.Value as ProblemDetails;
        Assert.IsTrue(result?.ContentTypes.Contains(MediaTypes.ProblemDetails) ?? false);
        Assert.AreEqual(result!.StatusCode, (int)HttpStatusCode.InternalServerError);
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
    public async Task HiramekuController_ExecuteAction()
    {
        var target = GetTarget();
        var controllerType = typeof(TestHiramekuController);
        var methodInfo = controllerType.GetMethod("ExecuteAction", BindingFlags.NonPublic | BindingFlags.Instance);
        var genericMethod = methodInfo?.MakeGenericMethod(typeof(object));

        Assert.IsNotNull(genericMethod);

        Func<Task<IActionResult>> action = () => Task.FromResult(new OkResult() as IActionResult);
        var task = genericMethod.Invoke(target, new object[] { new object(), action }) as Task<IActionResult>;

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
            var task = genericMethod.Invoke(target, new object[] { new object(), null! }) as Task;

            await task!.ConfigureAwait(false);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task HiramekuController_GetSecurityToken()
    {
        const string UserName = nameof(UserName);
        const string UserId = nameof(UserId);
        const string Name = nameof(Name);
        var expected = TestUtilities.GetJwtSecurityToken(UserName, UserId, Name);
        var target = GetTarget(TestUtilities.GetMockContextAccessor(expected));
        var controllerType = typeof(TestHiramekuController);
        var methodInfo = controllerType.GetMethod("GetSecurityToken", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(methodInfo);

        var task = methodInfo.Invoke(target, Array.Empty<object>()) as Task<JwtSecurityToken>;

        Assert.IsNotNull(task);

        var actual = await task.ConfigureAwait(false);

        Assert.AreEqual(expected.RawData, actual.RawData);
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

    private static HiramekuController GetTarget(Mock<IHttpContextAccessor>? mockContextAccessor = default)
    {
        return new TestHiramekuController(mockContextAccessor?.Object ?? Mock.Of<IHttpContextAccessor>());
    }

    private static async Task RunAuthorizeAndExecuteActionTest<TResult>(
        Mock<IHttpContextAccessor>? mockContextAccessor = default)
        where TResult : class, IActionResult
    {
        var target = GetTarget(mockContextAccessor);
        var controllerType = typeof(TestHiramekuController);
        var methodInfo = controllerType.GetMethod(
            "AuthorizeAndExecuteAction",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var genericMethod = methodInfo?.MakeGenericMethod(typeof(object));

        Assert.IsNotNull(genericMethod);

        Func<ClaimsPrincipal, Task<IActionResult>> action = _ => Task.FromResult(new OkResult() as IActionResult);
        var task = genericMethod.Invoke(target, new object[] { new object(), action }) as Task<IActionResult>;

        Assert.IsNotNull(task);

        var result = await task.ConfigureAwait(false);

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<TResult>(result);
    }
}
