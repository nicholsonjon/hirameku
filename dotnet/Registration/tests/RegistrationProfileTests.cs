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
using Hirameku.Common;
using Hirameku.Data;
using Hirameku.Registration;
using CommonPasswordValidationResult = Hirameku.Common.Service.PasswordValidationResult;
using ServicePasswordValidationResult = Hirameku.Registration.PasswordValidationResult;

[TestClass]
public class RegistrationProfileTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegistrationProfile_AssertConfigurationIsValid()
    {
        var config = new MapperConfiguration(c => c.AddProfile<RegistrationProfile>());
        config.AssertConfigurationIsValid();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegistrationProfile_Constructor()
    {
        var target = new RegistrationProfile();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(CommonPasswordValidationResult.Blacklisted, ServicePasswordValidationResult.Blacklisted)]
    [DataRow(CommonPasswordValidationResult.InsufficientEntropy, ServicePasswordValidationResult.InsufficientEntropy)]
    [DataRow(CommonPasswordValidationResult.TooLong, ServicePasswordValidationResult.TooLong)]
    [DataRow(CommonPasswordValidationResult.Valid, ServicePasswordValidationResult.Valid)]
    public void RegistrationProfile_Map_CommonPasswordValidationResult_ServicePasswordValidationResult(
        CommonPasswordValidationResult source,
        ServicePasswordValidationResult expected)
    {
        var target = GetTarget();

        var actual = target.Map<ServicePasswordValidationResult>(source);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegistrationProfile_Map_RegisterModel_UserDocument()
    {
        var source = new RegisterModel()
        {
            EmailAddress = nameof(RegisterModel.EmailAddress),
            Name = nameof(RegisterModel.Name),
            UserName = nameof(RegisterModel.UserName),
        };
        var target = GetTarget();

        var destination = target.Map<UserDocument>(source);

        Assert.AreEqual(source.EmailAddress, destination.EmailAddress);
        Assert.AreEqual(source.Name, destination.Name);
        Assert.AreEqual(source.UserName, destination.UserName);
        Assert.AreEqual(UserStatus.EmailNotVerified, destination.UserStatus);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(VerificationTokenVerificationResult.NotVerified, EmailVerificationResult.NotVerified)]
    [DataRow(VerificationTokenVerificationResult.TokenExpired, EmailVerificationResult.TokenExpired)]
    [DataRow(VerificationTokenVerificationResult.Verified, EmailVerificationResult.Verified)]
    public void RegistrationProfile_Map_VerificationTokenVerificationResult_EmailVerificationResult(
        VerificationTokenVerificationResult source,
        EmailVerificationResult expected)
    {
        var target = GetTarget();

        var actual = target.Map<EmailVerificationResult>(source);

        Assert.AreEqual(expected, actual);
    }

    private static IMapper GetTarget()
    {
        var configuration = new MapperConfiguration(c => c.AddProfile<RegistrationProfile>());
        return configuration.CreateMapper();
    }
}
