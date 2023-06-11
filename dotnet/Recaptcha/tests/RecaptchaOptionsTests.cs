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

namespace Hirameku.Recaptcha.Tests;

[TestClass]
public class RecaptchaOptionsTests
{
    private const int MaxRetries = 5;
    private const double MinimumScore = 1.0d;
    private const string SiteSecret = nameof(SiteSecret);
    private static readonly TimeSpan MedianFirstRetryDelay = new(0, 0, 1);
    private static readonly Uri VerificationUrl = new("http://localhost");

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaOptions_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaOptions_MaxRetries()
    {
        var target = GetTarget();

        Assert.AreEqual(MaxRetries, target.MaxRetries);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaOptions_MedianFirstRetryDelay()
    {
        var target = GetTarget();

        Assert.AreEqual(MedianFirstRetryDelay, target.MedianFirstRetryDelay);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaOptions_MinimumScore()
    {
        var target = GetTarget();

        Assert.AreEqual(MinimumScore, target.MinimumScore);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaOptions_SiteSecret()
    {
        var target = GetTarget();

        Assert.AreEqual(SiteSecret, target.SiteSecret);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaOptions_VerificationUrl()
    {
        var target = GetTarget();

        Assert.AreEqual(VerificationUrl, target.VerificationUrl);
    }

    private static RecaptchaOptions GetTarget()
    {
        return new RecaptchaOptions()
        {
            MaxRetries = MaxRetries,
            MedianFirstRetryDelay = MedianFirstRetryDelay,
            MinimumScore = MinimumScore,
            SiteSecret = SiteSecret,
            VerificationUrl = VerificationUrl,
        };
    }
}
