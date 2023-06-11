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

[TestClass]
public class SecurityTokenOptionsTests
{
    private const string SecretKey = nameof(SecretKey);
    private const string SecurityAlgorithm = nameof(SecurityAlgorithm);
    private static readonly Uri Localhost = new("http://localhost");
    private static readonly TimeSpan TokenExpiry = new(0, 30, 0);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SecurityTokenOptions_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SecurityTokenOptions_Audience()
    {
        var target = GetTarget();

        Assert.AreEqual(Localhost, target.Audience);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SecurityTokenOptions_Issuer()
    {
        var target = GetTarget();

        Assert.AreEqual(Localhost, target.Issuer);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SecurityTokenOptions_SecretKey()
    {
        var target = GetTarget();

        Assert.AreEqual(SecretKey, target.SecretKey);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SecurityTokenOptions_SecurityAlgorithm()
    {
        var target = GetTarget();

        Assert.AreEqual(SecurityAlgorithm, target.SecurityAlgorithm);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SecurityTokenOptions_TokenExpiry()
    {
        var target = GetTarget();

        Assert.AreEqual(TokenExpiry, target.TokenExpiry);
    }

    private static SecurityTokenOptions GetTarget()
    {
        return new SecurityTokenOptions()
        {
            Audience = Localhost,
            Issuer = Localhost,
            SecretKey = SecretKey,
            SecurityAlgorithm = SecurityAlgorithm,
            TokenExpiry = TokenExpiry,
        };
    }
}
