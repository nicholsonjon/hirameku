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
public class RecaptchaResponseTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaResponse_Action()
    {
        const string Action = nameof(RecaptchaResponse.Action);

        var target = new RecaptchaResponse()
        {
            Action = Action,
        };

        Assert.AreEqual(Action, target.Action);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaResponse_Constructor()
    {
        var target = new RecaptchaResponse();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaResponse_ChallengeTimestamp()
    {
        var challengeTimestamp = DateTime.UtcNow;

        var target = new RecaptchaResponse()
        {
            ChallengeTimestamp = challengeTimestamp,
        };

        Assert.AreEqual(challengeTimestamp, target.ChallengeTimestamp);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaResponse_ErrorCodes()
    {
        var errorCodes = new List<string>();

        var target = new RecaptchaResponse()
        {
            ErrorCodes = errorCodes,
        };

        Assert.AreEqual(errorCodes, target.ErrorCodes);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaResponse_Hostname()
    {
        const string Hostname = nameof(RecaptchaResponse.Hostname);

        var target = new RecaptchaResponse()
        {
            Hostname = Hostname,
        };

        Assert.AreEqual(Hostname, target.Hostname);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaResponse_Score()
    {
        var score = 1.0d;

        var target = new RecaptchaResponse()
        {
            Score = score,
        };

        Assert.AreEqual(score, target.Score);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaResponse_Success()
    {
        const bool Success = true;

        var target = new RecaptchaResponse()
        {
            Success = Success,
        };

        Assert.AreEqual(Success, target.Success);
    }
}
