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

using Hirameku.TestTools;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

[TestClass]
public class SecurityTokenIssuerTests
{
    public const string Name = nameof(Name);
    public const string UserId = nameof(UserId);
    public const string UserName = nameof(UserName);
    public const string SecurityAlgorithm = SecurityAlgorithms.HmacSha512;
    public static readonly Uri Localhost = new("http://localhost");
    public static readonly DateTime Now = DateTime.UtcNow;
    public static readonly string SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    public static readonly TimeSpan TokenExpiry = TimeSpan.FromMinutes(30);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SecurityTokenIssuer_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SecurityTokenIssuser_Issue()
    {
        var target = GetTarget();

        var token = target.Issue(
            UserId,
            new User()
            {
                Name = Name,
                UserName = UserName,
            });

        Assert.AreEqual(GetJwtSecurityToken().ToString(), token.ToString());
    }

    private static JwtSecurityToken GetJwtSecurityToken()
    {
        var localhost = Localhost.ToString();

        return TestUtilities.GetJwtSecurityToken(
            localhost,
            UserName,
            UserId,
            Name,
            Now,
            localhost,
            TokenExpiry,
            SecretKey,
            SecurityAlgorithm);
    }

    private static Mock<IDateTimeProvider> GetMockDateTimeProvider()
    {
        var mockProvider = new Mock<IDateTimeProvider>();
        _ = mockProvider.Setup(m => m.UtcNow)
            .Returns(Now);

        return mockProvider;
    }

    private static Mock<IOptions<SecurityTokenOptions>> GetMockOptions()
    {
        var mockOptions = new Mock<IOptions<SecurityTokenOptions>>();
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new SecurityTokenOptions()
            {
                Audience = Localhost,
                Issuer = Localhost,
                SecretKey = SecretKey,
                SecurityAlgorithm = SecurityAlgorithm,
                TokenExpiry = TokenExpiry,
            });

        return mockOptions;
    }

    private static SecurityTokenIssuer GetTarget()
    {
        return new SecurityTokenIssuer(GetMockDateTimeProvider().Object, GetMockOptions().Object);
    }
}
