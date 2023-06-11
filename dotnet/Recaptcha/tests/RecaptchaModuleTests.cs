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

using Autofac;
using Microsoft.Extensions.Options;
using Moq;

[TestClass]
public class RecaptchaModuleTests
{
    private static readonly Lazy<IContainer> Target = new(() =>
    {
        var builder = new ContainerBuilder();
        _ = builder.RegisterModule<RecaptchaModule>();

        // we mock the configuration-based dependencies because these come from the IServiceCollection and are not
        // directly managed by the IoC container
        _ = builder.Register(_ => Mock.Of<IOptions<RecaptchaOptions>>());
        _ = builder.Register(_ => Mock.Of<HttpClient>());

        return builder.Build();
    });

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaModule_Constructor()
    {
        var target = new RecaptchaModule();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaModule_Load_IRecaptchaClient()
    {
        var client = Target.Value.Resolve<IRecaptchaClient>();

        Assert.IsNotNull(client);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RecaptchaModule_Load_IRecaptchaResponseValidator()
    {
        var validator = Target.Value.Resolve<IRecaptchaResponseValidator>();

        Assert.IsNotNull(validator);
    }
}
