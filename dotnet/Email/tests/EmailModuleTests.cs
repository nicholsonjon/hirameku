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

using Autofac;
using FluentValidation;
using Hirameku.Common;
using Microsoft.Extensions.Options;
using Moq;

[TestClass]
public class EmailModuleTests
{
    private static readonly Lazy<IContainer> Target = new(() =>
    {
        var builder = new ContainerBuilder();
        _ = builder.RegisterModule<EmailModule>();

        // we mock the configuration-based dependencies because these come from the IServiceCollection and are not
        // directly managed by the IoC container
        _ = builder.Register(_ => Mock.Of<IOptions<EmailerOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<VerificationOptions>>());

        return builder.Build();
    });

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailModule_Constructor()
    {
        var target = new EmailModule();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailModule_Load_IEmailer()
    {
        var emailer = Target.Value.Resolve<IEmailer>();

        Assert.IsNotNull(emailer);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailModule_Load_IEmailTokenDataValidator()
    {
        var validator = Target.Value.Resolve<IValidator<EmailTokenData>>();

        Assert.IsNotNull(validator);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void EmailModule_Load_IEmailTokenSerializer()
    {
        var serializer = Target.Value.Resolve<IEmailTokenSerializer>();

        Assert.IsNotNull(serializer);
    }
}
