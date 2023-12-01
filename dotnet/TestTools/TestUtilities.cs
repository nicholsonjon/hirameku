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

namespace Hirameku.TestTools;

using Hirameku.Common.Service;
using Hirameku.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

public static class TestUtilities
{
    public const string Name = nameof(Name);
    public const string SecurityAlgorithm = SecurityAlgorithms.HmacSha512;
    public const string UserId = nameof(UserId);
    public const string UserName = nameof(UserName);
    public static readonly Uri Localhost = new("http://localhost");
    public static readonly DateTime Now = DateTime.UtcNow;
    public static readonly TimeSpan TokenExpiry = TimeSpan.FromMinutes(30);
    public static readonly string SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    private const string AccessTokenName = ".Token.access_token";

    public static void AssertExpressionFilter<TDocument>(FilterDefinition<TDocument> filter, TDocument document)
    {
        ArgumentNullException.ThrowIfNull(filter);

        Assert.IsInstanceOfType(filter, typeof(ExpressionFilterDefinition<TDocument>));
        AssertExpressionFilter(((ExpressionFilterDefinition<TDocument>)filter).Expression, document);
    }

    public static void AssertExpressionFilter<TDocument>(Expression<Func<TDocument, bool>> filter, TDocument document)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var expression = filter.Compile();
        Assert.IsTrue(expression(document));
    }

    public static void AssertMemberExpression<TMember, TValue>(
        Expression<Func<TMember, TValue>> expression,
        string memberName)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var lambda = expression as LambdaExpression;
        var body = lambda.Body as MemberExpression;
        var member = body?.Member;
        var parameter = lambda.Parameters.SingleOrDefault();

        Assert.IsNotNull(body);
        Assert.IsNotNull(member);
        Assert.IsNotNull(parameter);
        Assert.AreEqual(ExpressionType.MemberAccess, body!.NodeType);
        Assert.AreEqual(memberName, member!.Name);
        Assert.AreEqual(typeof(TMember), parameter!.Type);
    }

    public static void AssertProjection<TDocument, TValue>(
        ProjectionDefinition<TDocument, TValue> projection,
        TDocument document,
        TValue value)
    {
        ArgumentNullException.ThrowIfNull(projection);

        var type = projection.GetType();
        var propertyValue = type?.GetProperty("Expression")?.GetValue(projection, null)
            as Expression<Func<TDocument, TValue>>;

        Assert.IsNotNull(propertyValue);

        var expression = propertyValue.Compile();
        Assert.AreEqual(expression(document), value);
    }

    public static void AssertUpdate<TDocument, TValue>(
        FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update,
        TDocument document,
        string updatedFieldName,
        TValue? expectedValue)
    {
        AssertUpdate<TDocument, TValue>(
            filter,
            update,
            document,
            updatedFieldName,
            (v) => Assert.AreEqual(expectedValue, v));
    }

    public static void AssertUpdate<TDocument, TValue>(
        FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update,
        TDocument document,
        string updatedFieldName,
        Action<TValue> assertValueAction)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(assertValueAction);

        var type = update.GetType();
        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        var field = type.GetField("_field", bindingFlags)?.GetValue(update)
            as FieldDefinition<TDocument, TValue>;
        var opName = type.GetField("_operatorName", bindingFlags)?.GetValue(update) as string;
        var renderedField = field?.Render(
            BsonSerializer.LookupSerializer<TDocument>(),
            BsonSerializer.SerializerRegistry);
        var value = type.GetField("_value", bindingFlags)?.GetValue(update);

        Assert.IsInstanceOfType(filter, typeof(ExpressionFilterDefinition<TDocument>));
        var expression = ((ExpressionFilterDefinition<TDocument>)filter).Expression.Compile();
        Assert.IsTrue(expression(document));
        Assert.AreEqual("$set", opName);
        Assert.AreEqual(updatedFieldName, renderedField?.FieldName);
        Assert.IsNotNull(value);
        assertValueAction((TValue)value);
    }

    public static double CalculateBase64Length(int bytes)
    {
        var encodedBytes = Math.Ceiling(bytes * 1.33);
        return encodedBytes + Math.Abs((encodedBytes % 4) - 4);
    }

    public static async Task<VerificationToken> GenerateToken(
        DateTime? creationDate = default,
        string? emailAddress = default,
        DateTime? expirationDate = default)
    {
        var now = DateTime.UtcNow;
        var verification = new Verification()
        {
            CreationDate = creationDate ?? now,
            EmailAddress = emailAddress ?? nameof(Verification.EmailAddress),
            ExpirationDate = expirationDate ?? now,
            Salt = new byte[] { 0 },
        };

        return await VerificationToken.Create(verification, new byte[] { 0 }, HashAlgorithmName.MD5)
            .ConfigureAwait(false);
    }

    public static JwtSecurityToken GetJwtSecurityToken(
        string userName = UserName,
        string userId = UserId,
        string name = Name)
    {
        var localhost = Localhost.ToString();
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Convert.FromBase64String(SecretKey)),
            SecurityAlgorithm);
        var descriptor = new SecurityTokenDescriptor
        {
            Audience = localhost,
            Claims = new Dictionary<string, object>()
            {
                { JwtRegisteredClaimNames.Name, name },
                { JwtRegisteredClaimNames.Sub, userName },
                { PrivateClaims.UserId, userId },
            },
            Expires = Now + TokenExpiry,
            IssuedAt = Now,
            Issuer = localhost,
            NotBefore = Now,
            SigningCredentials = signingCredentials,
        };

        var handler = new JwtSecurityTokenHandler();

        return handler.CreateJwtSecurityToken(descriptor);
    }

    [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "MD5 is employed here to generate a unique key that is not used for any security-critical purposes")]
    public static string GetMD5HexString(params string[] strings)
    {
        var builder = new StringBuilder();
        (strings ?? Enumerable.Empty<string>()).ToList().ForEach(s => builder.Append(s));

        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    public static Mock<IHttpContextAccessor> GetMockContextAccessor(
        JwtSecurityToken? securityToken = default,
        ClaimsPrincipal? user = default)
    {
        var authenticationTicket = new AuthenticationTicket(
            new GenericPrincipal(Mock.Of<IIdentity>(), default),
            new AuthenticationProperties(new Dictionary<string, string?>()
            {
                { AccessTokenName, securityToken?.RawData },
            }),
            JwtBearerDefaults.AuthenticationScheme);
        var mockAuthenticationService = new Mock<IAuthenticationService>();
        var context = new DefaultHttpContext();
        _ = mockAuthenticationService.Setup(m => m.AuthenticateAsync(context, default))
            .ReturnsAsync(AuthenticateResult.Success(authenticationTicket));
        var mockRequestServices = new Mock<IServiceProvider>();
        _ = mockRequestServices.Setup(m => m.GetService(typeof(IAuthenticationService)))
            .Returns(mockAuthenticationService.Object);

        context.RequestServices = mockRequestServices.Object;

        if (user != null)
        {
            context.User = user;
        }

        var mockAccessor = new Mock<IHttpContextAccessor>();
        _ = mockAccessor.Setup(m => m.HttpContext)
            .Returns(context);

        return mockAccessor;
    }

    public static ClaimsPrincipal GetUser(string userId = UserId)
    {
        var identity = new ClaimsIdentity(new Claim[] { new(PrivateClaims.UserId, userId) }, "JwtBearer");

        return new ClaimsPrincipal(identity);
    }

    public static bool IsUpdateFor<TDocument, TValue>(UpdateDefinition<TDocument> update, string updatedFieldName)
    {
        ArgumentNullException.ThrowIfNull(update);

        var type = update.GetType();
        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        var field = type.GetField("_field", bindingFlags)?.GetValue(update)
            as FieldDefinition<TDocument, TValue?>;
        var renderedField = field?.Render(
            BsonSerializer.LookupSerializer<TDocument>(),
            BsonSerializer.SerializerRegistry);

        return renderedField?.FieldName == updatedFieldName;
    }
}
