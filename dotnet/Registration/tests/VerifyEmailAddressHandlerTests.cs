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
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.TestTools;
using Moq;
using System.Linq.Expressions;
using CommonPasswordValidationResult = Hirameku.Common.Service.PasswordValidationResult;

[TestClass]
public class VerifyEmailAddressHandlerTests
{
    private const string EmailAddress = "test@test.local";
    private const string Password = TestData.Password;
    private const string Pepper = TestData.Pepper;
    private const string SerializedToken = TestData.SerializedToken;
    private const string Token = TestData.Token;
    private const string UserId = nameof(UserId);
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void VerifyEmailAddressHandler_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(VerificationTokenVerificationResult.NotVerified, EmailVerificationResult.NotVerified)]
    [DataRow(VerificationTokenVerificationResult.TokenExpired, EmailVerificationResult.TokenExpired)]
    [DataRow(VerificationTokenVerificationResult.Verified, EmailVerificationResult.Verified)]
    public async Task VerifyEmailAddressHandler_VerifyEmail(
        VerificationTokenVerificationResult tokenResult,
        EmailVerificationResult expectedResult)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = GetMockUserDao(GetUser(), cancellationToken);
        var mockVerificationDao = GetMockVerificationDao(tokenResult, cancellationToken);
        var target = GetTarget(
            mockEmailTokenSerializer: GetMockEmailTokenSerializer(),
            mockUserDao: mockUserDao,
            mockVerificationDao: mockVerificationDao);

        var actualResult = await target.VerifyEmaiAddress(SerializedToken, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task VerifyEmailAddressHandler_VerifyEmail_UserDoesNotExist_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        _ = mockUserDao.Setup(m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, t) => TestUtilities.AssertExpressionFilter(f, GetUser()))
            .ReturnsAsync(null as UserDocument);
        var target = GetTarget(mockEmailTokenSerializer: GetMockEmailTokenSerializer(), mockUserDao: mockUserDao);

        _ = await target.VerifyEmaiAddress(SerializedToken, cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    private static Mock<IEmailTokenSerializer> GetMockEmailTokenSerializer()
    {
        var mockSerializer = new Mock<IEmailTokenSerializer>();
        mockSerializer.Setup(m => m.Deserialize(SerializedToken))
            .Returns(new Tuple<string, string, string>(Pepper, Token, UserName))
            .Verifiable();

        return mockSerializer;
    }

    private static Mock<IMapper> GetMockMapper(RegisterModel model, UserDocument document)
    {
        var mockMapper = new Mock<IMapper>();
        _ = mockMapper.Setup(m => m.Map<UserDocument>(model))
            .Returns(document);

        return mockMapper;
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

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDao(
        UserDocument user,
        CancellationToken cancellationToken)
    {
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();

        _ = mockUserDao.Setup(
            m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, ct) => TestUtilities.AssertExpressionFilter(f, user))
            .ReturnsAsync(user);

        return mockUserDao;
    }

    private static Mock<IVerificationDao> GetMockVerificationDao(
        VerificationTokenVerificationResult verificationResult,
        CancellationToken cancellationToken = default)
    {
        var mockVerificationDao = new Mock<IVerificationDao>();
        mockVerificationDao.Setup(m => m.VerifyToken(
            UserId,
            EmailAddress,
            VerificationType.EmailVerification,
            Token,
            Pepper,
            cancellationToken))
            .ReturnsAsync(verificationResult)
            .Verifiable();

        return mockVerificationDao;
    }

    private static VerifyEmailAddressHandler GetTarget(
        Mock<IEmailTokenSerializer>? mockEmailTokenSerializer = default,
        Mock<IMapper>? mockMapper = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default,
        Mock<IVerificationDao>? mockVerificationDao = default)
    {
        var mapper = mockMapper?.Object
            ?? new MapperConfiguration(c => c.AddProfile<RegistrationProfile>()).CreateMapper();

        return new VerifyEmailAddressHandler(
            mockEmailTokenSerializer?.Object ?? Mock.Of<IEmailTokenSerializer>(),
            mapper,
            mockUserDao?.Object ?? Mock.Of<IDocumentDao<UserDocument>>(),
            mockVerificationDao?.Object ?? Mock.Of<IVerificationDao>());
    }

    private static UserDocument GetUser()
    {
        return new UserDocument()
        {
            EmailAddress = EmailAddress,
            Id = UserId,
            UserName = UserName,
            UserStatus = UserStatus.OK,
        };
    }
}
