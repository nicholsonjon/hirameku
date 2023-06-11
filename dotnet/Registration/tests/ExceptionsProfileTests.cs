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

namespace Hirameku.Registration.Tests;

using AutoMapper;
using FluentValidation;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Common.Service.Properties;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Microsoft.AspNetCore.Mvc;
using System.Net;

[TestClass]
public class ExceptionsProfileTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ExceptionsProfile_AssertConfigurationIsValid()
    {
        var configuration = new MapperConfiguration(c => c.AddProfile<ExceptionsProfile>());
        configuration.AssertConfigurationIsValid();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ExceptionsProfile_Constructor()
    {
        var target = new ExceptionsProfile();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ExceptionsProfile_Map_ArgumentException_ProblemDetails()
    {
        const string Message = nameof(Message);
        var exception = new ArgumentException(Message);
        var target = GetTarget();

        var problemDetails = target.Map<ProblemDetails>(exception);

        Assert.AreEqual(Message, problemDetails.Detail);
        Assert.IsTrue(problemDetails.Extensions.Count == 0);
        Assert.AreEqual(ErrorCodes.RequestValidationFailed, problemDetails.Instance);
        Assert.AreEqual((int)HttpStatusCode.BadRequest, problemDetails.Status);
        Assert.AreEqual(Resources.RequestValidationFailed, problemDetails.Title);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ExceptionsProfile_Map_EmailAddressAlreadyVerifiedException_ProblemDetails()
    {
        var message = Resources.EmailAddressAlreadyVerified;
        var exception = new EmailAddressAlreadyVerifiedException(message);
        var target = GetTarget();

        var problemDetails = target.Map<ProblemDetails>(exception);

        Assert.AreEqual(message, problemDetails.Detail);
        Assert.IsTrue(problemDetails.Extensions.Count == 0);
        Assert.AreEqual(ErrorCodes.EmailAddressAlreadyVerified, problemDetails.Instance);
        Assert.AreEqual((int)HttpStatusCode.BadRequest, problemDetails.Status);
        Assert.AreEqual(Resources.EmailAddressAlreadyVerified, problemDetails.Title);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ExceptionsProfile_Map_EmailAddressNotVerifiedException_ProblemDetails()
    {
        var message = Resources.EmailAddressNotVerified;
        var exception = new EmailAddressNotVerifiedException(message);
        var target = GetTarget();

        var problemDetails = target.Map<ProblemDetails>(exception);

        Assert.AreEqual(message, problemDetails.Detail);
        Assert.IsTrue(problemDetails.Extensions.Count == 0);
        Assert.AreEqual(ErrorCodes.EmailAddressNotVerified, problemDetails.Instance);
        Assert.AreEqual((int)HttpStatusCode.Forbidden, problemDetails.Status);
        Assert.AreEqual(Resources.EmailAddressNotVerified, problemDetails.Title);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ExceptionsProfile_Map_InvalidTokenException_ProblemDetails()
    {
        var message = Resources.InvalidToken;
        var exception = new InvalidTokenException(message);
        var target = GetTarget();

        var problemDetails = target.Map<ProblemDetails>(exception);

        Assert.AreEqual(message, problemDetails.Detail);
        Assert.IsTrue(problemDetails.Extensions.Count == 0);
        Assert.AreEqual(ErrorCodes.InvalidToken, problemDetails.Instance);
        Assert.AreEqual((int)HttpStatusCode.BadRequest, problemDetails.Status);
        Assert.AreEqual(Resources.InvalidToken, problemDetails.Title);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ExceptionsProfile_Map_PasswordException_ProblemDetails()
    {
        var message = Resources.PasswordChangeRejected;
        var exception = new PasswordException(message);
        var target = GetTarget();

        var problemDetails = target.Map<ProblemDetails>(exception);

        Assert.AreEqual(message, problemDetails.Detail);
        Assert.IsTrue(problemDetails.Extensions.Count == 0);
        Assert.AreEqual(ErrorCodes.PasswordChangeRejected, problemDetails.Instance);
        Assert.AreEqual((int)HttpStatusCode.BadRequest, problemDetails.Status);
        Assert.AreEqual(Resources.PasswordChangeRejected, problemDetails.Title);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ExceptionsProfile_Map_RecaptchaVerificationFailedException_ProblemDetails()
    {
        var message = Resources.RecaptchaVerificationFailed;
        var exception = new RecaptchaVerificationFailedException(message);
        var target = GetTarget();

        var problemDetails = target.Map<ProblemDetails>(exception);

        Assert.AreEqual(message, problemDetails.Detail);
        Assert.IsTrue(problemDetails.Extensions.Count == 0);
        Assert.AreEqual(ErrorCodes.RecaptchaVerificationFailed, problemDetails.Instance);
        Assert.AreEqual((int)HttpStatusCode.BadRequest, problemDetails.Status);
        Assert.AreEqual(Resources.RecaptchaVerificationFailed, problemDetails.Title);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ExceptionsProfile_Map_UserAlreadyExistsException_ProblemDetails()
    {
        var message = Resources.UserAlreadyExists;
        var exception = new UserAlreadyExistsException(message);
        var target = GetTarget();

        var problemDetails = target.Map<ProblemDetails>(exception);

        Assert.AreEqual(message, problemDetails.Detail);
        Assert.IsTrue(problemDetails.Extensions.Count == 0);
        Assert.AreEqual(ErrorCodes.UserAlreadyExists, problemDetails.Instance);
        Assert.AreEqual((int)HttpStatusCode.BadRequest, problemDetails.Status);
        Assert.AreEqual(Resources.UserAlreadyExists, problemDetails.Title);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ExceptionsProfile_Map_UserDoesNotExistException_ProblemDetails()
    {
        var message = Resources.UserDoesNotExist;
        var exception = new UserDoesNotExistException(message);
        var target = GetTarget();

        var problemDetails = target.Map<ProblemDetails>(exception);

        Assert.AreEqual(message, problemDetails.Detail);
        Assert.IsTrue(problemDetails.Extensions.Count == 0);
        Assert.AreEqual(ErrorCodes.UserDoesNotExist, problemDetails.Instance);
        Assert.AreEqual((int)HttpStatusCode.BadRequest, problemDetails.Status);
        Assert.AreEqual(Resources.UserDoesNotExist, problemDetails.Title);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ExceptionsProfile_Map_UserSuspendedException_ProblemDetails()
    {
        var message = Resources.UserSuspended;
        var exception = new UserSuspendedException(message);
        var target = GetTarget();

        var problemDetails = target.Map<ProblemDetails>(exception);

        Assert.AreEqual(message, problemDetails.Detail);
        Assert.IsTrue(problemDetails.Extensions.Count == 0);
        Assert.AreEqual(ErrorCodes.UserSuspended, problemDetails.Instance);
        Assert.AreEqual((int)HttpStatusCode.Forbidden, problemDetails.Status);
        Assert.AreEqual(Resources.UserSuspended, problemDetails.Title);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ExceptionsProfile_Map_ValidationException_ProblemDetails()
    {
        const string Message = nameof(Message);
        var exception = new ValidationException(Message);
        var target = GetTarget();

        var problemDetails = target.Map<ProblemDetails>(exception);

        Assert.AreEqual(Message, problemDetails.Detail);
        Assert.IsTrue(problemDetails.Extensions.Count == 0);
        Assert.AreEqual(ErrorCodes.RequestValidationFailed, problemDetails.Instance);
        Assert.AreEqual((int)HttpStatusCode.BadRequest, problemDetails.Status);
        Assert.AreEqual(Resources.RequestValidationFailed, problemDetails.Title);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ExceptionsProfile_Map_VerificationException_ProblemDetails()
    {
        var message = Resources.VerificationFailed;
        var exception = new VerificationException(message);
        var target = GetTarget();

        var problemDetails = target.Map<ProblemDetails>(exception);

        Assert.AreEqual(message, problemDetails.Detail);
        Assert.IsTrue(problemDetails.Extensions.Count == 0);
        Assert.AreEqual(ErrorCodes.VerificationFailed, problemDetails.Instance);
        Assert.AreEqual((int)HttpStatusCode.BadRequest, problemDetails.Status);
        Assert.AreEqual(Resources.VerificationFailed, problemDetails.Title);
    }

    private static IMapper GetTarget()
    {
        var configuration = new MapperConfiguration(c => c.AddProfile<ExceptionsProfile>());
        return configuration.CreateMapper();
    }
}
