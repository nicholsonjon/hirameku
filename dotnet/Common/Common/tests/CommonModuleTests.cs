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

using Autofac;

[TestClass]
public class CommonModuleTests
{
    private static readonly Lazy<IContainer> Target = new(() =>
    {
        var builder = new ContainerBuilder();
        _ = builder.RegisterModule<CommonModule>();

        return builder.Build();
    });

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CommonModule_Constructor()
    {
        var target = new CommonModuleTests();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CommonModule_Load_Base64StringValidator()
    {
        var validator = Target.Value.Resolve<Base64StringValidator>();

        Assert.IsNotNull(validator);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CommonModule_Load_EmailAddressValidator()
    {
        var validator = Target.Value.Resolve<EmailAddressValidator>();

        Assert.IsNotNull(validator);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CommonModule_Load_IDateTimeProvider()
    {
        var dateTimeProvider = Target.Value.Resolve<IDateTimeProvider>();

        Assert.IsNotNull(dateTimeProvider);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CommonModule_Load_IGuidProvider()
    {
        var guidProvider = Target.Value.Resolve<IGuidProvider>();

        Assert.IsNotNull(guidProvider);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CommonModule_Load_IUniqueIdGenerator()
    {
        var uniqueIdGenerator = Target.Value.Resolve<IUniqueIdGenerator>();

        Assert.IsNotNull(uniqueIdGenerator);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CommonModule_Load_NameValidator()
    {
        var validator = Target.Value.Resolve<NameValidator>();

        Assert.IsNotNull(validator);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CommonModule_Load_UserNameValidator()
    {
        var validator = Target.Value.Resolve<UserNameValidator>();

        Assert.IsNotNull(validator);
    }
}
