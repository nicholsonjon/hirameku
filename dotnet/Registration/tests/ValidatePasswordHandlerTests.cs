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
using Hirameku.Common.Service;
using Hirameku.TestTools;
using Moq;
using CommonPasswordValidationResult = Hirameku.Common.Service.PasswordValidationResult;
using ServicePasswordValidationResult = Hirameku.Registration.PasswordValidationResult;

[TestClass]
public class ValidatePasswordHandlerTests
{
    private const string Password = TestData.Password;
    private const string UserId = nameof(UserId);
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ValidatePasswordHandler_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task ValidatePasswordHandler_ValidatePassword()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var target = GetTarget(mockPasswordValidator: GetMockPasswordValidator(
            cancellationToken: cancellationToken));

        var actual = await target.ValidatePassword(Password, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(ServicePasswordValidationResult.Valid, actual);
    }

    private static Mock<IPasswordValidator> GetMockPasswordValidator(
        CommonPasswordValidationResult result = CommonPasswordValidationResult.Valid,
        CancellationToken cancellationToken = default)
    {
        var mockPasswordValidator = new Mock<IPasswordValidator>();
        mockPasswordValidator.Setup(m => m.Validate(Password, cancellationToken))
            .ReturnsAsync(result)
            .Verifiable();

        return mockPasswordValidator;
    }

    private static ValidatePasswordHandler GetTarget(
        Mock<IMapper>? mockMapper = default,
        Mock<IPasswordValidator>? mockPasswordValidator = default)
    {
        var mapper = mockMapper?.Object
            ?? new MapperConfiguration(c => c.AddProfile<RegistrationProfile>()).CreateMapper();
        var passwordValidator = mockPasswordValidator?.Object ?? Mock.Of<IPasswordValidator>();

        return new ValidatePasswordHandler(mapper, passwordValidator);
    }
}
