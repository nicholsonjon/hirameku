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

namespace Hirameku.Common.Tests;

using System.Security.Cryptography;

[TestClass]
public class VerificationOptionsTests
{
    private const int PepperLength = 32;
    private const int SaltLength = 64;
    private static readonly HashAlgorithmName HashName = HashAlgorithmName.SHA256;
    private static readonly TimeSpan MaxVerificationAge = TimeSpan.FromDays(1);
    private static readonly TimeSpan MinVerificationAge = TimeSpan.FromMinutes(5);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void VerificationOptions_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void VerificationOptions_GetHashName()
    {
        var target = GetTarget();

        Assert.AreEqual(HashName, target.HashName);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void VerificationOptions_GetMaxVerificationAge()
    {
        var target = GetTarget();

        Assert.AreEqual(MaxVerificationAge, target.MaxVerificationAge);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void VerificationOptions_GetMinVerificationAge()
    {
        var target = GetTarget();

        Assert.AreEqual(MinVerificationAge, target.MinVerificationAge);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void VerificationOptions_GetPepperLength()
    {
        var target = GetTarget();

        Assert.AreEqual(PepperLength, target.PepperLength);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void VerificationOptions_GetSaltLength()
    {
        var target = GetTarget();

        Assert.AreEqual(SaltLength, target.SaltLength);
    }

    private static VerificationOptions GetTarget()
    {
        return new VerificationOptions()
        {
            HashName = HashName,
            MaxVerificationAge = MaxVerificationAge,
            MinVerificationAge = MinVerificationAge,
            PepperLength = PepperLength,
            SaltLength = SaltLength,
        };
    }
}
