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

[TestClass]
public class EmailerOptionsTests
{
    private const string FeedbackEmailAddress = nameof(FeedbackEmailAddress);
    private const string QueryStringParameterName = nameof(QueryStringParameterName);
    private const string Sender = nameof(Sender);
    private const string SmtpPassword = nameof(SmtpPassword);
    private const int SmtpPort = 25;
    private const string SmtpServer = nameof(SmtpServer);
    private const string SmtpUserName = nameof(SmtpUserName);
    private const bool UseTls = true;
    private static readonly Uri RejectRegistrationUrl = new("http://localhost/rejectregistration");
    private static readonly Uri ResetPasswordUrl = new("http://localhost/resetpassword");
    private static readonly Uri VerifyEmailUrl = new("http://localhost/verifyemail");

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailerOptionsTests_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailerOptions_FeedbackEmailAddress()
    {
        var target = GetTarget();

        Assert.AreEqual(FeedbackEmailAddress, target.FeedbackEmailAddress);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailerOptions_QueryStringParameterName()
    {
        var target = GetTarget();

        Assert.AreEqual(QueryStringParameterName, target.QueryStringParameterName);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailerOptions_RejectRegistrationUrl()
    {
        var target = GetTarget();

        Assert.AreEqual(RejectRegistrationUrl, target.RejectRegistrationUrl);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailerOptions_ResetPasswordUrl()
    {
        var target = GetTarget();

        Assert.AreEqual(ResetPasswordUrl, target.ResetPasswordUrl);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailerOptions_Sender()
    {
        var target = GetTarget();

        Assert.AreEqual(Sender, target.Sender);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailerOptions_SmtpPassword()
    {
        var target = GetTarget();

        Assert.AreEqual(SmtpPassword, target.SmtpPassword);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailerOptions_SmtpPort()
    {
        var target = GetTarget();

        Assert.AreEqual(SmtpPort, target.SmtpPort);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailerOptions_SmtpServer()
    {
        var target = GetTarget();

        Assert.AreEqual(SmtpServer, target.SmtpServer);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailerOptions_SmtpUserName()
    {
        var target = GetTarget();

        Assert.AreEqual(SmtpUserName, target.SmtpUserName);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailerOptions_UseTls()
    {
        var target = GetTarget();

        Assert.AreEqual(UseTls, target.UseTls);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailerOptions_VerifyEmailUrl()
    {
        var target = GetTarget();

        Assert.AreEqual(VerifyEmailUrl, target.VerifyEmailUrl);
    }

    private static EmailerOptions GetTarget()
    {
        return new EmailerOptions()
        {
            FeedbackEmailAddress = FeedbackEmailAddress,
            QueryStringParameterName = QueryStringParameterName,
            RejectRegistrationUrl = RejectRegistrationUrl,
            ResetPasswordUrl = ResetPasswordUrl,
            Sender = Sender,
            SmtpPassword = SmtpPassword,
            SmtpPort = SmtpPort,
            SmtpServer = SmtpServer,
            SmtpUserName = SmtpUserName,
            UseTls = UseTls,
            VerifyEmailUrl = VerifyEmailUrl,
        };
    }
}
