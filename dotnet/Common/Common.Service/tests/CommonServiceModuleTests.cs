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

using Autofac;
using Hirameku.Data;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using Nito.AsyncEx;

[TestClass]
public class CommonServiceModuleTests
{
    private static readonly Lazy<IContainer> Target = new(() => GetContainerBuilder().Build());

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CommonServiceModule_Constructor()
    {
        var target = new CommonServiceModule();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CommonServiceModule_Load_AsyncLazyOfIEnumerableOfString()
    {
        var asyncLazy = Target.Value.Resolve<AsyncLazy<IEnumerable<string>>>();

        Assert.IsNotNull(asyncLazy);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task CommonServiceModule_Load_AsyncLazyOfIEnumerableOfString_Integration()
    {
        var builder = GetContainerBuilder();
        _ = builder.Register(
            c =>
            {
                var mockOptions = new Mock<IOptions<PasswordValidatorOptions>>();
                _ = mockOptions.Setup(m => m.Value)
                    .Returns(new PasswordValidatorOptions()
                    {
                        PasswordBlacklistPath = "password-blacklist.txt"
                    });

                return mockOptions.Object;
            });

        var target = builder.Build();
        var asyncLazy = target.Resolve<AsyncLazy<IEnumerable<string>>>();

        Assert.IsNotNull(asyncLazy);

        var blacklist = await asyncLazy.ConfigureAwait(false);

        Assert.AreEqual(10, blacklist?.Count() ?? 0);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CommonServiceModule_Load_IPasswordValidator()
    {
        var passwordValidator = Target.Value.Resolve<IPasswordValidator>();

        Assert.IsNotNull(passwordValidator);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CommonServiceModule_Load_IPersistentTokenIssuer()
    {
        var persistentTokenIssuer = Target.Value.Resolve<IPersistentTokenIssuer>();

        Assert.IsNotNull(persistentTokenIssuer);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CommonServiceModule_Load_ISecurityTokenIssuer()
    {
        var securityTokenIssuer = Target.Value.Resolve<ISecurityTokenIssuer>();

        Assert.IsNotNull(securityTokenIssuer);
    }

    private static ContainerBuilder GetContainerBuilder()
    {
        var builder = new ContainerBuilder();
        _ = builder.RegisterModule<CommonServiceModule>();

        // we mock the configuration-based dependencies because these come from the IServiceCollection and are not
        // directly managed by the IoC container
        _ = builder.Register(_ => Mock.Of<IOptions<DatabaseOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<PasswordOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<PasswordValidatorOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<PersistentTokenOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<SecurityTokenOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<VerificationOptions>>());

        // IMongoDatabase must be mocked because the registration delegate takes out connections to the database
        var mockDatabase = new Mock<IMongoDatabase>();
        _ = mockDatabase.Setup(m => m.GetCollection<UserDocument>(UserDocument.CollectionName, default))
            .Returns(Mock.Of<IMongoCollection<UserDocument>>());

        _ = builder.RegisterInstance(mockDatabase.Object);

        return builder;
    }
}
