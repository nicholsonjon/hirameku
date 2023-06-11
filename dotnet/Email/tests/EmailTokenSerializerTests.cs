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

namespace Hirameku.Email.Tests;

using Hirameku.Common;
using Hirameku.TestTools;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Cryptography;
using System.Text;

[TestClass]
public class EmailTokenSerializerTests
{
    private const string Pepper = TestData.Pepper;
    private const int PepperLength = 32;
    private const string SerializedToken = "UGVwcGVyVG9rZW5Sx6s1qZ4=";
    private const string Token = TestData.Token;
    private const string UserName = nameof(UserName);
    private static readonly TimeSpan MaxVerificationAge = TimeSpan.FromDays(1);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailTokenSerializer_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidTokenException))]
    public void EmailTokenSerializer_Deserialize_SerializedToken_IsNotBase64()
    {
        var target = GetTarget();

        _ = target.Deserialize("!@#$");

        Assert.Fail(nameof(InvalidTokenException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidTokenException))]
    public void EmailTokenSerializer_Deserialize_SerializedToken_IsNull()
    {
        var target = GetTarget();

        _ = target.Deserialize(null!);

        Assert.Fail(nameof(InvalidTokenException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(InvalidTokenException))]
    public void EmailTokenSerializer_Deserialize_SerializedToken_LengthIsInvalid()
    {
        var target = GetTarget();

        _ = target.Deserialize(Convert.ToBase64String(Encoding.UTF8.GetBytes(UserName)));

        Assert.Fail(nameof(InvalidTokenException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailTokenSerializer_Serialize()
    {
        var target = GetTarget();

        var serializedToken = target.Serialize(Pepper, Token, UserName);

        Assert.AreEqual(SerializedToken, serializedToken);
    }

    private static Mock<IOptions<VerificationOptions>> GetMockOptions()
    {
        var mockOptions = new Mock<IOptions<VerificationOptions>>();
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new VerificationOptions()
            {
                HashName = HashAlgorithmName.SHA512,
                MaxVerificationAge = MaxVerificationAge,
                MinVerificationAge = default,
                PepperLength = PepperLength,
                SaltLength = default,
            });

        return mockOptions;
    }

    private static EmailTokenSerializer GetTarget(Mock<IOptions<VerificationOptions>>? mockOptions = default)
    {
        return new EmailTokenSerializer((mockOptions ?? GetMockOptions()).Object);
    }
}
