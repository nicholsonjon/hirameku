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

namespace Hirameku.Caching.Tests;

using Autofac;
using Hirameku.Common;
using Hirameku.Data;
using Hirameku.TestTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

[TestClass]
public class CachingModuleTests
{
    private const string AppSettingsFilename = "appsettings.json";
    private static readonly TimeSpan OperationTimeout = new(0, 0, 5);
    private static readonly Lazy<IContainer> Target = new(() =>
    {
        var builder = new ContainerBuilder();
        _ = builder.RegisterModule<CachingModule>();

        // we mock the configuration-based dependencies because these come from the IServiceCollection and are not
        // directly managed by the IoC container
        _ = builder.Register(_ => Mock.Of<IConfiguration>());
        _ = builder.Register(_ => Mock.Of<IOptions<CacheOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<DatabaseOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<PasswordOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<PersistentTokenOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<VerificationOptions>>());

        // IMongoDatabase must be mocked because the registration delegate takes out connections to the database
        var mockDatabase = new Mock<IMongoDatabase>();
        _ = mockDatabase.Setup(m => m.GetCollection<UserDocument>(UserDocument.CollectionName, default))
            .Returns(Mock.Of<IMongoCollection<UserDocument>>());
        _ = builder.RegisterInstance(mockDatabase.Object);

        // ICacheClientFactory must be mocked because its registration delegate takes out connections to the database
        var mockCacheClientFactory = new Mock<ICacheClientFactory>();
        _ = mockCacheClientFactory.Setup(m => m.CreateClient())
            .Returns(Mock.Of<ICacheClient>());
        _ = builder.RegisterInstance(mockCacheClientFactory.Object);

        return builder.Build();
    });

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CachingModule_Constructor()
    {
        var target = new CachingModule();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CachingModule_Load_ICacheClient()
    {
        var cache = Target.Value.Resolve<ICacheClient>();

        Assert.IsNotNull(cache);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CachingModule_Load_ICacheClientFactory()
    {
        var cache = Target.Value.Resolve<ICacheClientFactory>();

        Assert.IsNotNull(cache);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    [DoNotParallelize]
    public async Task CachingModule_Load_ICacheClientFactory_ConfigurationChangeTracking()
    {
        var mockOptions = new Mock<IOptions<CacheOptions>>();
        var appSettings = await IntegrationTestUtilities.GetAppSettings(AppSettingsFilename).ConfigureAwait(false);
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new CacheOptions()
            {
                ConnectionString = appSettings.Hirameku.Caching.CacheOptions.ConnectionString,
                OperationTimeout = OperationTimeout,
            });

        await IntegrationTestUtilities.RunConfigurationChangeTrackingTest<CachingModule, ICacheClientFactory, CacheOptions>(
            mockOptions.Object,
            () => IntegrationTestUtilities.ModifyAppSettingsFile(
                AppSettingsFilename,
                s => s.Hirameku.Caching.CacheOptions.OperationTimeout = OperationTimeout),
            () => IntegrationTestUtilities.ModifyAppSettingsFile(
                AppSettingsFilename,
                s => s.Hirameku.Caching.CacheOptions.OperationTimeout = OperationTimeout + TimeSpan.FromSeconds(1)))
            .ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CachingModule_Load_IDocumentDaoOfUserDocument()
    {
        var dao = Target.Value.Resolve<IDocumentDao<UserDocument>>();

        Assert.IsNotNull(dao);
    }
}
