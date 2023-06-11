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

using Autofac;
using FluentValidation;
using Hirameku.Common;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Microsoft.Extensions.Options;
using Moq;

namespace Hirameku.Contact.Tests;

[TestClass]
public class ContactModuleTests
{
    private static readonly Lazy<IContainer> Target = new(() =>
    {
        var builder = new ContainerBuilder();
        _ = builder.RegisterModule<ContactModule>();

        // we mock the configuration-based dependencies because these come from the IServiceCollection and are not
        // directly managed by the IoC container
        _ = builder.Register(_ => Mock.Of<IOptions<EmailerOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<RecaptchaOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<VerificationOptions>>());
        _ = builder.Register(_ => Mock.Of<HttpClient>());

        return builder.Build();
    });

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ContactModule_Constructor()
    {
        var target = new ContactModule();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ContactModule_Load_IContactProvider()
    {
        var provider = Target.Value.Resolve<IContactProvider>();

        Assert.IsNotNull(provider);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ContactModule_Load_IValidatorOfSendFeedbackModel()
    {
        var validator = Target.Value.Resolve<IValidator<SendFeedbackModel>>();

        Assert.IsNotNull(validator);
    }
}
