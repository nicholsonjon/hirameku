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

namespace Hirameku.User.Tests;

using FluentValidation.TestHelper;
using Hirameku.Common.Service;
using Moq;

[TestClass]
public class ChangePasswordModelValidatorTests
{
    private const string CurrentPassword = nameof(CurrentPassword);
    private const string NewPassword = nameof(NewPassword);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ChangePasswordModelValidator_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, DisplayName = nameof(ChangePasswordModelValidator_CurrentPassword_NullEmptyOrWhiteSpace) + "(null)")]
    [DataRow("", DisplayName = nameof(ChangePasswordModelValidator_CurrentPassword_NullEmptyOrWhiteSpace) + "(string.Empty)")]
    [DataRow("\t\r\n ", DisplayName = nameof(ChangePasswordModelValidator_CurrentPassword_NullEmptyOrWhiteSpace) + "(WhiteSpace)")]
    public async Task ChangePasswordModelValidator_CurrentPassword_NullEmptyOrWhiteSpace(string currentPassword)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var target = GetTarget(GetMockPasswordValidator(cancellationToken: cancellationToken));

        var result = await target.TestValidateAsync(GetModel(currentPassword), default, cancellationToken)
            .ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.CurrentPassword).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(PasswordValidationResult.Blacklisted)]
    [DataRow(PasswordValidationResult.InsufficientEntropy)]
    [DataRow(PasswordValidationResult.TooLong)]
    public async Task ChangePasswordModelValidator_NewPassword_NotValid(PasswordValidationResult validationResult)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var target = GetTarget(GetMockPasswordValidator(validationResult, cancellationToken));

        var result = await target.TestValidateAsync(GetModel(), default, cancellationToken).ConfigureAwait(false);

        _ = result.ShouldHaveValidationErrorFor(m => m.NewPassword).Only();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task ChangePasswordModelValidator_Validate()
    {
        var target = GetTarget(GetMockPasswordValidator());

        var result = await target.TestValidateAsync(GetModel()).ConfigureAwait(false);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static Mock<IPasswordValidator> GetMockPasswordValidator(
        PasswordValidationResult result = PasswordValidationResult.Valid,
        CancellationToken cancellationToken = default)
    {
        var mockValidator = new Mock<IPasswordValidator>();
        _ = mockValidator.Setup(m => m.Validate(NewPassword, cancellationToken))
            .ReturnsAsync(result);

        return mockValidator;
    }

    private static ChangePasswordModel GetModel(string currentPassword = CurrentPassword)
    {
        return new ChangePasswordModel()
        {
            CurrentPassword = currentPassword,
            NewPassword = NewPassword,
        };
    }

    private static ChangePasswordModelValidator GetTarget(Mock<IPasswordValidator>? mockPasswordValidator = default)
    {
        return new ChangePasswordModelValidator(mockPasswordValidator?.Object ?? Mock.Of<IPasswordValidator>());
    }
}
