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

using FluentValidation;
using FluentValidation.Results;
using Moq;
using System.Reflection;

[TestClass]
public class IPasswordValidatorExtensionsTests
{
    private const string Password = nameof(Password);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task IPasswordValidatorExtensions_ContextIsNull_Throws()
    {
        await IPasswordValidatorExtensions.ValidateAsync<TestModel>(
            GetMockPasswordValidator(PasswordValidationResult.Valid, default).Object,
            Password,
            null!,
            default)
            .ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task IPasswordValidatorExtensions_InstanceIsNull_Throws()
    {
        await IPasswordValidatorExtensions.ValidateAsync(
            null!,
            Password,
            new ValidationContext<TestModel>(new TestModel()),
            default)
            .ConfigureAwait(false);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task IPasswordValidatorExtensions_Validate_InvalidPasswordValidationResult_Throws()
    {
        var mockValidator = GetMockPasswordValidator((PasswordValidationResult)(-1), default);
        var context = new ValidationContext<TestModel>(new TestModel());

        await IPasswordValidatorExtensions.ValidateAsync(mockValidator.Object, Password, context, default)
            .ConfigureAwait(false);

        Assert.Fail(nameof(InvalidOperationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(PasswordValidationResult.Blacklisted)]
    [DataRow(PasswordValidationResult.InsufficientEntropy)]
    [DataRow(PasswordValidationResult.TooLong)]
    public async Task IPasswordValidatorExtensions_Validate_PasswordIsInvalid(PasswordValidationResult result)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockValidator = GetMockPasswordValidator(result, cancellationToken);
        var context = new ValidationContext<TestModel>(new TestModel());

        await IPasswordValidatorExtensions.ValidateAsync(
            mockValidator.Object,
            Password,
            context,
            cancellationToken)
            .ConfigureAwait(false);

        Assert.IsTrue(HasFailure(context));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task IPasswordValidatorExtensions_Validate()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockValidator = GetMockPasswordValidator(PasswordValidationResult.Valid, cancellationToken);
        var context = new ValidationContext<TestModel>(new TestModel());

        await IPasswordValidatorExtensions.ValidateAsync(
            mockValidator.Object,
            Password,
            context,
            cancellationToken)
            .ConfigureAwait(false);

        Assert.IsFalse(HasFailure(context));
    }

    private static Mock<IPasswordValidator> GetMockPasswordValidator(
        PasswordValidationResult result,
        CancellationToken cancellationToken)
    {
        var mockValidator = new Mock<IPasswordValidator>();
        _ = mockValidator.Setup(m => m.Validate(Password, cancellationToken))
            .ReturnsAsync(result);

        return mockValidator;
    }

    private static bool HasFailure(IValidationContext context)
    {
        // I hate Microsoft for not porting PrivateObject/PrivateType to .NET Core
        var type = context.GetType();
        var bindingFlags = BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic;
        var property = type.GetProperty("Failures", bindingFlags);
        var failures = property?.GetValue(context) as List<ValidationFailure>;

        return failures?.Count == 1;
    }
}
