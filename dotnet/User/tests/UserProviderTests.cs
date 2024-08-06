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

using FluentValidation;
using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.TestTools;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;

[TestClass]
public class UserProviderTests
{
    private const string CurrentPassword = nameof(CurrentPassword);
    private const string EmailAddress = "test@test.local";
    private const string Name = nameof(Name);
    private const string NewPassword = nameof(NewPassword);
    private const string UserId = "1234567890abcdef12345678";
    private const string UserName = nameof(UserName);
    private static readonly TimeSpan MaxVerificationAge = TimeSpan.FromDays(1);
    private static readonly DateTime Now = DateTime.UtcNow;

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void UserProvider_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(false)]
    [DataRow(true)]
    public async Task UserProvider_ChangePassword(bool rememberMe)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var user = GetUser();
        var mockUserDao = GetMockUserDao(user, cancellationToken);
        var mockPasswordDao = GetMockPasswordDao(cancellationToken: cancellationToken);
        var mockPersistentTokenIssuer = GetMockPersistentTokenIssuer(cancellationToken);
        var mockSecurityTokenIssuer = GetMockSecurityTokenIssuer(user);
        var target = GetTarget(
            mockPasswordDao: mockPasswordDao,
            mockPersistentTokenIssuer: mockPersistentTokenIssuer,
            mockSecurityTokenIssuer: mockSecurityTokenIssuer,
            mockUserDao: mockUserDao,
            cancellationToken: cancellationToken);

        var responseModel = await target.ChangePassword(GetChangePasswordModel(rememberMe), cancellationToken)
            .ConfigureAwait(false);

        Assert.IsNotNull(responseModel?.SessionToken);
        mockUserDao.Verify();
        mockPasswordDao.Verify();
        mockSecurityTokenIssuer.Verify();

        if (rememberMe)
        {
            var persistentToken = responseModel?.PersistentToken;
            Assert.IsTrue(rememberMe ? persistentToken != null : persistentToken == null);
            mockPersistentTokenIssuer.Verify();
        }
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task UserProvider_ChangePassword_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        _ = await target.ChangePassword(
            new Authenticated<ChangePasswordModel>(
                new ChangePasswordModel(),
                new ClaimsPrincipal()))
            .ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(UserStatus.EmailNotVerifiedAndPasswordChangeRequired, UserStatus.EmailNotVerified)]
    [DataRow(UserStatus.PasswordChangeRequired, UserStatus.OK)]
    public async Task UserProvider_ChangePassword_PasswordChangeRequired(
        UserStatus currentStatus,
        UserStatus updatedStatus)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var user = GetUser(currentStatus);
        var mockUserDao = GetMockUserDao(user, cancellationToken);
        var mockPasswordDao = GetMockPasswordDao(cancellationToken: cancellationToken);
        var mockPersistentTokenIssuer = GetMockPersistentTokenIssuer(cancellationToken);
        var mockSecurityTokenIssuer = GetMockSecurityTokenIssuer(user);
        var mockCachedValueDao = new Mock<ICachedValueDao>();
        mockCachedValueDao.Setup(m => m.SetUserStatus(UserId, updatedStatus))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var target = GetTarget(
            mockCachedValueDao,
            mockPasswordDao: mockPasswordDao,
            mockPersistentTokenIssuer: mockPersistentTokenIssuer,
            mockSecurityTokenIssuer: mockSecurityTokenIssuer,
            mockUserDao: mockUserDao,
            cancellationToken: cancellationToken);

        var responseModel = await target.ChangePassword(GetChangePasswordModel(), cancellationToken)
            .ConfigureAwait(false);

        Assert.IsNotNull(responseModel?.SessionToken);
        mockUserDao.Verify();
        mockPasswordDao.Verify();
        mockSecurityTokenIssuer.Verify();
        mockCachedValueDao.Verify();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidPasswordException))]
    public async Task UserProvider_ChangePassword_PasswordNotVerified_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = GetMockUserDao(GetUser(), cancellationToken);
        var mockPasswordDao = GetMockPasswordDao(PasswordVerificationResult.NotVerified, cancellationToken);
        var target = GetTarget(
            mockPasswordDao: mockPasswordDao,
            mockUserDao: mockUserDao,
            cancellationToken: cancellationToken);

        _ = await target.ChangePassword(GetChangePasswordModel(), cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(InvalidPasswordException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task UserProvider_ChangePassword_UserDoesNotExist_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = GetMockUserDao(cancellationToken: cancellationToken);
        var target = GetTarget(cancellationToken: cancellationToken);

        _ = await target.ChangePassword(GetChangePasswordModel(), cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserProvider_DeleteUser()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockUserDao = new Mock<IDocumentDao<UserDocument>>();
        mockUserDao.Setup(m => m.Delete(UserId, cancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var target = GetTarget(mockUserDao: mockUserDao);
        var model = new Authenticated<Unit>(
            Unit.Value,
            new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, UserId) })));

        await target.DeleteUser(model, cancellationToken).ConfigureAwait(false);

        mockUserDao.Verify();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task UserProvider_DeleteUser_AuthenticatedModelIsNull_Throws()
    {
        var target = GetTarget();

        await target.DeleteUser(null!).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow("", DisplayName = nameof(UserProvider_DeleteUser_UserIdIsEmptyOrWhiteSpace_Throws) + "(string.Empty)")]
    [DataRow("\t\r\n ", DisplayName = nameof(UserProvider_DeleteUser_UserIdIsEmptyOrWhiteSpace_Throws) + "(WhiteSpace)")]
    public async Task UserProvider_DeleteUser_UserIdIsEmptyOrWhiteSpace_Throws(string userId)
    {
        var target = GetTarget();
        var model = new Authenticated<Unit>(
            Unit.Value,
            new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, userId) })));

        await target.DeleteUser(model).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserProvider_GetUser()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var expected = new UserDocument() { Id = UserId };
        var target = GetTarget(mockUserDao: GetMockUserDao(expected, cancellationToken));
        var model = new Authenticated<Unit>(
            Unit.Value,
            new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, UserId) })));

        var actual = await target.GetUser(model, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task UserProvider_GetUser_AuthenticatedModelIsNull_Throws()
    {
        var target = GetTarget();

        _ = await target.GetUser(null!).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentException))]
    [DataRow("", DisplayName = nameof(UserProvider_GetUser_UserIdEmptyOrWhiteSpace_Throws) + "(string.Empty)")]
    [DataRow("\t\r\n ", DisplayName = nameof(UserProvider_GetUser_UserIdEmptyOrWhiteSpace_Throws) + "(WhiteSpace)")]
    public async Task UserProvider_GetUser_UserIdEmptyOrWhiteSpace_Throws(string userId)
    {
        var target = GetTarget();
        var model = new Authenticated<Unit>(
            Unit.Value,
            new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, userId) })));

        _ = await target.GetUser(model).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public Task UserProvider_UpdateEmailAddress()
    {
        return RunUpdateEmailAddressTest(GetUser(), UserStatus.EmailNotVerified);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task UserProvider_UpdateEmailAddress_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        await target.UpdateEmailAddress(
            new Authenticated<UpdateEmailAddressModel>(
                new UpdateEmailAddressModel(),
                GetClaimsPrincipal()))
            .ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public Task UserProvider_UpdateEmailAddress_PasswordChangeRequired()
    {
        var user = GetUser();
        user.UserStatus = UserStatus.PasswordChangeRequired;

        return RunUpdateEmailAddressTest(user, UserStatus.EmailNotVerifiedAndPasswordChangeRequired);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task UserProvider_UpdateEmailAddress_UserDoesNotExist_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var target = GetTarget(mockUserDao: GetMockUserDao(cancellationToken: cancellationToken));

        await target.UpdateEmailAddress(
            new Authenticated<UpdateEmailAddressModel>(
                new UpdateEmailAddressModel() { EmailAddress = EmailAddress },
                GetClaimsPrincipal()),
            cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserProvider_UpdateName()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var user = new UserDocument() { Id = UserId };
        var mockUserDao = GetMockUserDaoForUpdateName(user, cancellationToken: cancellationToken);
        var expected = new JwtSecurityToken();
        var mockSecurityTokenIssuer = GetMockSecurityTokenIssuer(user, expected);
        var target = GetTarget(mockSecurityTokenIssuer: mockSecurityTokenIssuer, mockUserDao: mockUserDao);
        var model = new Authenticated<UpdateNameModel>(
            new UpdateNameModel() { Name = Name },
            new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, UserId) })));

        var actual = await target.UpdateName(model, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expected, actual);
        mockUserDao.Verify();
        mockSecurityTokenIssuer.Verify();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task UserProvider_UpdateName_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        _ = await target.UpdateName(new Authenticated<UpdateNameModel>(new UpdateNameModel(), new ClaimsPrincipal()))
            .ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task UserProvider_UpdateName_UserDoesNotExist_Throws()
    {
        var target = GetTarget();

        _ = await target.UpdateName(
            new Authenticated<UpdateNameModel>(
                new UpdateNameModel() { Name = Name },
                new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, "foo") }))))
            .ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task UserProvider_UpdateUserName()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var user = new UserDocument() { Id = UserId };
        var mockUserDao = GetMockUserDaoForUpdateUserName(user, cancellationToken);
        var expected = new JwtSecurityToken();
        var mockSecurityTokenIssuer = GetMockSecurityTokenIssuer(user, expected);
        var target = GetTarget(mockUserDao: mockUserDao, mockSecurityTokenIssuer: mockSecurityTokenIssuer);
        var model = new Authenticated<UpdateUserNameModel>(
            new UpdateUserNameModel() { UserName = UserName },
            new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, UserId) })));

        var actual = await target.UpdateUserName(model, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expected, actual);
        mockUserDao.Verify();
        mockSecurityTokenIssuer.Verify();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ValidationException))]
    public async Task UserProvider_UpdateUserName_ModelIsInvalid_Throws()
    {
        var target = GetTarget();

        _ = await target.UpdateUserName(
            new Authenticated<UpdateUserNameModel>(new UpdateUserNameModel(), new ClaimsPrincipal()))
            .ConfigureAwait(false);

        Assert.Fail(nameof(ValidationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(UserDoesNotExistException))]
    public async Task UserProvider_UpdateUserName_UserDoesNotExist_Throws()
    {
        var target = GetTarget();

        _ = await target.UpdateUserName(
            new Authenticated<UpdateUserNameModel>(
                new UpdateUserNameModel() { UserName = UserName },
                new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, "foo") }))))
            .ConfigureAwait(false);

        Assert.Fail(nameof(UserDoesNotExistException) + " expected");
    }

    private static Authenticated<ChangePasswordModel> GetChangePasswordModel(bool rememberMe = false)
    {
        return new Authenticated<ChangePasswordModel>(
            new ChangePasswordModel()
            {
                CurrentPassword = CurrentPassword,
                NewPassword = NewPassword,
                RememberMe = rememberMe,
            },
            new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, UserId) })));
    }

    private static ClaimsPrincipal GetClaimsPrincipal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, UserId) }));
    }

    private static Mock<ICachedValueDao> GetMockCachedValueDao(UserStatus userStatus)
    {
        var mockDao = new Mock<ICachedValueDao>();
        mockDao.Setup(m => m.SetUserStatus(UserId, userStatus))
            .Returns(Task.CompletedTask)
            .Verifiable();

        return mockDao;
    }

    private static Mock<IEmailer> GetMockEmailer(VerificationToken verificationToken)
    {
        var mockEmailer = new Mock<IEmailer>();
        mockEmailer.Setup(
            m => m.SendVerificationEmail(EmailAddress, Name, It.IsAny<EmailTokenData>(), CancellationToken.None))
            .Callback<string, string, EmailTokenData, CancellationToken>(
                (ea, n, td, ct) =>
                {
                    Assert.AreEqual(verificationToken.Pepper, td.Pepper);
                    Assert.AreEqual(verificationToken.Token, td.Token);
                    Assert.AreEqual(UserName, td.UserName);
                    Assert.AreEqual(MaxVerificationAge, td.ValidityPeriod);
                })
            .Returns(Task.CompletedTask)
            .Verifiable();

        return mockEmailer;
    }

    private static Mock<IPasswordDao> GetMockPasswordDao(
        PasswordVerificationResult result = PasswordVerificationResult.Verified,
        CancellationToken cancellationToken = default)
    {
        var mockDao = new Mock<IPasswordDao>();
        mockDao.Setup(m => m.VerifyPassword(UserId, CurrentPassword, cancellationToken))
            .ReturnsAsync(result)
            .Verifiable();

        return mockDao;
    }

    private static Mock<IPersistentTokenIssuer> GetMockPersistentTokenIssuer(
        CancellationToken cancellationToken = default)
    {
        var mockIssuer = new Mock<IPersistentTokenIssuer>();
        mockIssuer.Setup(m => m.Issue(UserId, cancellationToken))
            .ReturnsAsync(new PersistentTokenModel())
            .Verifiable();

        return mockIssuer;
    }

    private static Mock<ISecurityTokenIssuer> GetMockSecurityTokenIssuer(User user)
    {
        var mockIssuer = new Mock<ISecurityTokenIssuer>();
        mockIssuer.Setup(m => m.Issue(UserId, user))
            .Returns(new JwtSecurityToken())
            .Verifiable();

        return mockIssuer;
    }

    private static Mock<ISecurityTokenIssuer> GetMockSecurityTokenIssuer(User user, SecurityToken sessionToken)
    {
        var mockIssuer = new Mock<ISecurityTokenIssuer>();
        mockIssuer.Setup(m => m.Issue(UserId, user))
            .Returns(sessionToken ?? new JwtSecurityToken())
            .Verifiable();

        return mockIssuer;
    }

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDao(
        UserDocument? user = default,
        CancellationToken cancellationToken = default)
    {
        var mockDao = new Mock<IDocumentDao<UserDocument>>();
        mockDao.Setup(m => m.Fetch(It.IsAny<Expression<Func<UserDocument, bool>>>(), cancellationToken))
            .Callback<Expression<Func<UserDocument, bool>>, CancellationToken>(
                (f, _) => TestUtilities.AssertExpressionFilter(f, user ?? GetUser()))
            .ReturnsAsync(user)
            .Verifiable();

        return mockDao;
    }

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDaoForUpdateEmailAddress(
        UserDocument? user = default,
        CancellationToken cancellationToken = default)
    {
        var mockDao = GetMockUserDao(user, cancellationToken);
        mockDao.Setup(
            m => m.Update(UserId, It.IsAny<Expression<Func<UserDocument, string>>>(), EmailAddress, cancellationToken))
            .Callback<string, Expression<Func<UserDocument, string>>, string, CancellationToken>(
                (id, f, v, _) =>
                {
                    Assert.AreEqual(UserId, id);
                    TestUtilities.AssertMemberExpression(f, nameof(UserDocument.EmailAddress));
                    Assert.AreEqual(EmailAddress, v);
                })
            .Returns(Task.CompletedTask)
            .Verifiable();

        return mockDao;
    }

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDaoForUpdateName(
        UserDocument? user = default,
        CancellationToken cancellationToken = default)
    {
        var mockDao = GetMockUserDao(user, cancellationToken);
        mockDao.Setup(
            m => m.Update(UserId, It.IsAny<Expression<Func<UserDocument, string>>>(), Name, cancellationToken))
            .Callback<string, Expression<Func<UserDocument, string>>, string, CancellationToken>(
                (id, f, v, _) =>
                {
                    Assert.AreEqual(UserId, id);
                    TestUtilities.AssertMemberExpression(f, nameof(UserDocument.Name));
                    Assert.AreEqual(Name, v);
                })
            .Returns(Task.CompletedTask)
            .Verifiable();

        return mockDao;
    }

    private static Mock<IDocumentDao<UserDocument>> GetMockUserDaoForUpdateUserName(
        UserDocument? user = default,
        CancellationToken cancellationToken = default)
    {
        var mockDao = GetMockUserDao(user, cancellationToken);
        mockDao.Setup(
            m => m.Update(UserId, It.IsAny<Expression<Func<UserDocument, string>>>(), UserName, cancellationToken))
            .Callback<string, Expression<Func<UserDocument, string>>, string, CancellationToken>(
                (id, f, v, _) =>
                {
                    Assert.AreEqual(UserId, id);
                    TestUtilities.AssertMemberExpression(f, nameof(UserDocument.UserName));
                    Assert.AreEqual(UserName, v);
                })
            .Returns(Task.CompletedTask)
            .Verifiable();

        return mockDao;
    }

    private static Mock<IVerificationDao> GetMockVerificationDao(VerificationToken verificationToken)
    {
        var mockDao = new Mock<IVerificationDao>();
        mockDao.Setup(
            m => m.GenerateVerificationToken(
                UserId,
                EmailAddress,
                VerificationType.EmailVerification,
                CancellationToken.None))
            .ReturnsAsync(verificationToken)
            .Verifiable();

        return mockDao;
    }

    private static Mock<IOptions<VerificationOptions>> GetMockVerificationOptions()
    {
        var mockOptions = new Mock<IOptions<VerificationOptions>>();
        mockOptions.Setup(m => m.Value)
            .Returns(new VerificationOptions() { MaxVerificationAge = MaxVerificationAge })
            .Verifiable();

        return mockOptions;
    }

    private static UserProvider GetTarget(
        Mock<ICachedValueDao>? mockCachedValueDao = default,
        Mock<IEmailer>? mockEmailer = default,
        Mock<IPasswordDao>? mockPasswordDao = default,
        Mock<IPasswordValidator>? mockPasswordValidator = default,
        Mock<IPersistentTokenIssuer>? mockPersistentTokenIssuer = default,
        Mock<ISecurityTokenIssuer>? mockSecurityTokenIssuer = default,
        Mock<IDocumentDao<UserDocument>>? mockUserDao = default,
        Mock<IVerificationDao>? mockVerificationDao = default,
        Mock<IOptions<VerificationOptions>>? mockVerificationOptions = default,
        CancellationToken cancellationToken = default)
    {
        mockPasswordValidator = new Mock<IPasswordValidator>();
        _ = mockPasswordValidator.Setup(m => m.Validate(NewPassword, cancellationToken))
            .ReturnsAsync(PasswordValidationResult.Valid);

        return new UserProvider(
            mockCachedValueDao?.Object ?? Mock.Of<ICachedValueDao>(),
            new AuthenticatedValidator<ChangePasswordModel>(
                new ChangePasswordModelValidator(mockPasswordValidator.Object)),
            mockEmailer?.Object ?? Mock.Of<IEmailer>(),
            mockPasswordDao?.Object ?? Mock.Of<IPasswordDao>(),
            mockPersistentTokenIssuer?.Object ?? Mock.Of<IPersistentTokenIssuer>(),
            mockSecurityTokenIssuer?.Object ?? Mock.Of<ISecurityTokenIssuer>(),
            new AuthenticatedValidator<UpdateEmailAddressModel>(new UpdateEmailAddressModelValidator()),
            new AuthenticatedValidator<UpdateNameModel>(new UpdateNameModelValidator()),
            new AuthenticatedValidator<UpdateUserNameModel>(new UpdateUserNameModelValidator()),
            mockUserDao?.Object ?? Mock.Of<IDocumentDao<UserDocument>>(),
            mockVerificationDao?.Object ?? Mock.Of<IVerificationDao>(),
            mockVerificationOptions?.Object ?? Mock.Of<IOptions<VerificationOptions>>());
    }

    private static UserDocument GetUser(UserStatus userStatus = UserStatus.OK)
    {
        return new UserDocument()
        {
            Id = UserId,
            Name = Name,
            UserName = UserName,
            UserStatus = userStatus,
        };
    }

    private static async Task RunUpdateEmailAddressTest(UserDocument user, UserStatus newUserStatus)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockCachedValueDao = GetMockCachedValueDao(newUserStatus);
        var verificationToken = await TestUtilities.GenerateToken(Now, EmailAddress, Now + MaxVerificationAge)
            .ConfigureAwait(false);
        var mockEmailer = GetMockEmailer(verificationToken);
        var mockUserDao = GetMockUserDaoForUpdateEmailAddress(user, cancellationToken);
        var mockVerificationDao = GetMockVerificationDao(verificationToken);
        var mockVerificationOptions = GetMockVerificationOptions();
        var target = GetTarget(
            mockCachedValueDao,
            mockEmailer,
            mockUserDao: mockUserDao,
            mockVerificationDao: mockVerificationDao,
            mockVerificationOptions: mockVerificationOptions);

        await target.UpdateEmailAddress(
            new Authenticated<UpdateEmailAddressModel>(
                new UpdateEmailAddressModel() { EmailAddress = EmailAddress },
                GetClaimsPrincipal()),
            cancellationToken)
            .ConfigureAwait(false);

        mockCachedValueDao.Verify();
        mockEmailer.Verify();
        mockUserDao.Verify();
        mockVerificationDao.Verify();
        mockVerificationOptions.Verify();
    }
}
