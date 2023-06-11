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

namespace Hirameku.Authentication.Tests;

using AutoMapper;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using System.IdentityModel.Tokens.Jwt;
using CommonUtilities = Hirameku.TestTools.TestUtilities;

[TestClass]
public class AuthenticationProfileTests
{
    private const string Accept = nameof(Accept);
    private const string ContentEncoding = nameof(ContentEncoding);
    private const string ContentLanguage = nameof(ContentLanguage);
    private const string RemoteIP = nameof(RemoteIP);
    private const string UserAgent = nameof(UserAgent);
    private const string UserId = nameof(UserId);
    private const string UserName = nameof(UserName);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationProfile_AssertConfigurationIsValid()
    {
        var mapperConfiguration = new MapperConfiguration(c => c.AddProfile<AuthenticationProfile>());
        mapperConfiguration.AssertConfigurationIsValid();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationProfile_Constructor()
    {
        var target = new AuthenticationProfile();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationProfile_Map_AuthenticationDataOfRenewTokenModel_AuthenticationEvent()
    {
        var authenticationData = GetAuthenticationData(new RenewTokenModel() { UserId = UserId });
        var target = GetTarget();

        var authenticationEvent = target.Map<AuthenticationEvent>(authenticationData);

        AssertAuthenticationEvent(authenticationData, authenticationEvent);
        Assert.AreEqual(UserId, authenticationEvent.UserId);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationProfile_Map_AuthenticationDataOfSignInModel_AuthenticationEvent()
    {
        var authenticationData = GetAuthenticationData(new SignInModel() { UserName = UserName });
        var target = GetTarget();

        var authenticationEvent = target.Map<AuthenticationEvent>(authenticationData);

        AssertAuthenticationEvent(authenticationData, authenticationEvent);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(PasswordVerificationResult.NotVerified, AuthenticationResult.NotAuthenticated)]
    [DataRow(PasswordVerificationResult.VerifiedAndExpired, AuthenticationResult.PasswordExpired)]
    [DataRow(PasswordVerificationResult.Verified, AuthenticationResult.Authenticated)]
    public void AuthenticationProfile_Map_PasswordVerificationResult_AuthenticationResult(
        PasswordVerificationResult passwordResult,
        AuthenticationResult expected)
    {
        var target = GetTarget();

        var actual = target.Map<AuthenticationResult>(passwordResult);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AuthenticationProfile_Map_PasswordVerificationResult_AuthenticationResult_Throws()
    {
        var target = GetTarget();

        _ = target.Map<AuthenticationResult>((PasswordVerificationResult)(-1));

        Assert.Fail(nameof(InvalidOperationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(PersistentTokenVerificationResult.NoTokenAvailable, AuthenticationResult.NotAuthenticated)]
    [DataRow(PersistentTokenVerificationResult.NotVerified, AuthenticationResult.NotAuthenticated)]
    [DataRow(PersistentTokenVerificationResult.Verified, AuthenticationResult.Authenticated)]
    public void AuthenticationProfile_Map_PersistentTokenVerificationResult_AuthenticationResult(
        PersistentTokenVerificationResult verificationResult,
        AuthenticationResult expected)
    {
        var target = GetTarget();

        var actual = target.Map<AuthenticationResult>(verificationResult);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AuthenticationProfile_Map_PersistentTokenVerificationResult_AuthenticationResult_Throws()
    {
        var target = GetTarget();

        _ = target.Map<AuthenticationResult>((PersistentTokenVerificationResult)(-1));

        Assert.Fail(nameof(InvalidOperationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void AuthenticationProfile_Map_SignInResult_SignInResponseModel()
    {
        var result = new SignInResult(
            AuthenticationResult.Authenticated,
            new PersistentTokenModel(),
            new JwtSecurityToken());
        var target = GetTarget();

        var model = target.Map<TokenResponseModel>(result);

        Assert.AreEqual(result.PersistentToken, model.PersistentToken);
        Assert.AreEqual(result.SessionToken, model.SessionToken);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(VerificationTokenVerificationResult.NotVerified, ResetPasswordResult.TokenNotVerified)]
    [DataRow(VerificationTokenVerificationResult.TokenExpired, ResetPasswordResult.TokenExpired)]
    [DataRow(VerificationTokenVerificationResult.Verified, ResetPasswordResult.PasswordReset)]
    public void AuthenticationProfile_Map_VerificationTokenVerificationResult_ResetPasswordResult(
        VerificationTokenVerificationResult verificationResult,
        ResetPasswordResult expected)
    {
        var target = GetTarget();

        var actual = target.Map<ResetPasswordResult>(verificationResult);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AuthenticationProfile_Map_VerificationTokenVerificationResult_ResetPasswordResult_Throws()
    {
        var target = GetTarget();

        _ = target.Map<ResetPasswordResult>((VerificationTokenVerificationResult)(-1));

        Assert.Fail(nameof(InvalidOperationException) + " expected");
    }

    private static void AssertAuthenticationEvent<TModel>(
        AuthenticationData<TModel> authenticationData,
        AuthenticationEvent authenticationEvent)
        where TModel : class
    {
        Assert.AreEqual(Accept, authenticationEvent.Accept);
        Assert.AreEqual(default, authenticationEvent.AuthenticationResult);
        Assert.AreEqual(ContentEncoding, authenticationEvent.ContentEncoding);
        Assert.AreEqual(ContentLanguage, authenticationEvent.ContentLanguage);
        Assert.AreEqual(GetHash(authenticationData), authenticationEvent.Hash);
        Assert.AreEqual(string.Empty, authenticationEvent.Id);
        Assert.AreEqual(RemoteIP, authenticationEvent.RemoteIP);
        Assert.AreEqual(UserAgent, authenticationEvent.UserAgent);
    }

    private static AuthenticationData<TModel> GetAuthenticationData<TModel>(TModel model)
        where TModel : class
    {
        return new AuthenticationData<TModel>(
            Accept,
            ContentEncoding,
            ContentLanguage,
            model,
            RemoteIP,
            UserAgent);
    }

    private static string GetHash<TModel>(AuthenticationData<TModel> data)
        where TModel : class
    {
        return CommonUtilities.GetMD5HexString(
            data.Accept,
            data.ContentEncoding,
            data.ContentLanguage,
            data.RemoteIP,
            data.UserAgent);
    }

    private static IMapper GetTarget()
    {
        var mapperConfiguration = new MapperConfiguration(c => c.AddProfile<AuthenticationProfile>());
        return mapperConfiguration.CreateMapper();
    }
}
