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
public class PasswordValidatorOptionsTests
{
    private const int MaxPasswordLength = 128;
    private const double MinPasswordEntropy = 40d;
    private const string PasswordBlacklistPath = nameof(PasswordBlacklistPath);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordValidatorOptions_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordValidatorOptions_MaxPasswordLength()
    {
        var target = GetTarget();

        Assert.AreEqual(MaxPasswordLength, target.MaxPasswordLength);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordValidatorOptions_MinPasswordEntropy()
    {
        var target = GetTarget();

        Assert.AreEqual(MinPasswordEntropy, target.MinPasswordEntropy);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordValidatorOptions_PasswordBlacklistPath()
    {
        var target = GetTarget();

        Assert.AreEqual(PasswordBlacklistPath, target.PasswordBlacklistPath);
    }

    private static PasswordValidatorOptions GetTarget()
    {
        return new PasswordValidatorOptions()
        {
            MaxPasswordLength = MaxPasswordLength,
            MinPasswordEntropy = MinPasswordEntropy,
            PasswordBlacklistPath = PasswordBlacklistPath,
        };
    }
}
