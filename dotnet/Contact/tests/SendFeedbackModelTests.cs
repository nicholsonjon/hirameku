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

namespace Hirameku.Contact.Tests;

[TestClass]
public class SendFeedbackModelTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SendFeedbackModel_Constructor()
    {
        var target = new SendFeedbackModel();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SendEmailAddressModel_EmailAddress()
    {
        const string EmailAddress = nameof(EmailAddress);

        var target = new SendFeedbackModel() { EmailAddress = EmailAddress, };

        Assert.AreEqual(EmailAddress, target.EmailAddress);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SendFeedbackModel_Feedback()
    {
        const string Feedback = nameof(Feedback);

        var target = new SendFeedbackModel() { Feedback = Feedback, };

        Assert.AreEqual(Feedback, target.Feedback);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SendNameModel_Name()
    {
        const string Name = nameof(Name);

        var target = new SendFeedbackModel() { Name = Name, };

        Assert.AreEqual(Name, target.Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SendRecaptchaResponseModel_RecaptchaResponse()
    {
        const string RecaptchaResponse = nameof(RecaptchaResponse);

        var target = new SendFeedbackModel() { RecaptchaResponse = RecaptchaResponse, };

        Assert.AreEqual(RecaptchaResponse, target.RecaptchaResponse);
    }
}
