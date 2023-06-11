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
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

public static class TestUtilities
{
    public static void AssertExpressionFilter<TDocument>(FilterDefinition<TDocument> filter, TDocument document)
    {
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        Assert.IsInstanceOfType(filter, typeof(ExpressionFilterDefinition<TDocument>));
        AssertExpressionFilter(((ExpressionFilterDefinition<TDocument>)filter).Expression, document);
    }

    public static void AssertExpressionFilter<TDocument>(Expression<Func<TDocument, bool>> filter, TDocument document)
    {
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        var expression = filter.Compile();
        Assert.IsTrue(expression(document));
    }

    public static void AssertMemberExpression<TMember, TValue>(
        Expression<Func<TMember, TValue>> expression,
        string memberName)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

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
        if (projection == null)
        {
            throw new ArgumentNullException(nameof(projection));
        }

        Assert.IsInstanceOfType(projection, typeof(FindExpressionProjectionDefinition<TDocument, TValue>));
        var expression = ((FindExpressionProjectionDefinition<TDocument, TValue>)projection).Expression.Compile();
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
        Action<TValue?> assertValueAction)
    {
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        if (update == null)
        {
            throw new ArgumentNullException(nameof(update));
        }

        if (assertValueAction == null)
        {
            throw new ArgumentNullException(nameof(assertValueAction));
        }

        var type = update.GetType();
        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        var field = type.GetField("_field", bindingFlags)?.GetValue(update)
            as FieldDefinition<TDocument, TValue?>;
        var opName = type.GetField("_operatorName", bindingFlags)?.GetValue(update) as string;
        var renderedField = field?.Render(
            BsonSerializer.LookupSerializer<TDocument>(),
            BsonSerializer.SerializerRegistry);
        var value = (TValue?)type.GetField("_value", bindingFlags)?.GetValue(update);

        Assert.IsInstanceOfType(filter, typeof(ExpressionFilterDefinition<TDocument>));
        var expression = ((ExpressionFilterDefinition<TDocument>)filter).Expression.Compile();
        Assert.IsTrue(expression(document));
        Assert.AreEqual("$set", opName);
        Assert.AreEqual(updatedFieldName, renderedField?.FieldName);
        assertValueAction(value);
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
        string audience,
        string userName,
        string userId,
        string name,
        DateTime now,
        string issuer,
        TimeSpan tokenExpiry,
        string secretKey,
        string securityAlgorithm)
    {
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Convert.FromBase64String(secretKey)),
            securityAlgorithm);
        var descriptor = new SecurityTokenDescriptor
        {
            Audience = audience,
            Claims = new Dictionary<string, object>()
            {
                { JwtRegisteredClaimNames.Name, name },
                { JwtRegisteredClaimNames.Sub, userName },
                { PrivateClaims.UserId, userId },
            },
            Expires = now + tokenExpiry,
            IssuedAt = now,
            Issuer = issuer,
            NotBefore = now,
            SigningCredentials = signingCredentials,
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);

        return token;
    }

    [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "MD5 is employed here to generate a unique key that is not used for any security-critical purposes")]
    public static string GetMD5HexString(params string[] strings)
    {
        var builder = new StringBuilder();
        (strings ?? Enumerable.Empty<string>()).ToList().ForEach(s => builder.Append(s));

        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    public static bool IsUpdateFor<TDocument, TValue>(UpdateDefinition<TDocument> update, string updatedFieldName)
    {
        if (update == null)
        {
            throw new ArgumentNullException(nameof(update));
        }

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
