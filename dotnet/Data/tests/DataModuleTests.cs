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

namespace Hirameku.Data.Tests;

using Autofac;
using FluentValidation;
using Hirameku.Common;
using Hirameku.TestTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

[TestClass]
public class DataModuleTests
{
    private const string AppSettingsFilename = "appsettings.json";
    private const string DatabaseName = "Test";
    private static readonly Lazy<IContainer> Target = new(() =>
    {
        var builder = new ContainerBuilder();
        _ = builder.RegisterModule<DataModule>();

        // we mock the configuration-based dependencies because these come from the IServiceCollection and are not
        // directly managed by the IoC container
        _ = builder.Register(_ => Mock.Of<IConfiguration>());
        _ = builder.Register(_ => Mock.Of<IOptions<DatabaseOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<PasswordOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<PersistentTokenOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<VerificationOptions>>());

        // IMongoDatabase must be mocked because the registration delegate takes out connections to the database
        var mockDatabase = new Mock<IMongoDatabase>();

        void SetupGetCollection<TDocument>(Mock<IMongoDatabase> mock, string collectionName)
        {
            _ = mock.Setup(m => m.GetCollection<TDocument>(collectionName, default))
                .Returns(Mock.Of<IMongoCollection<TDocument>>());
        }

        SetupGetCollection<AuthenticationEvent>(mockDatabase, AuthenticationEvent.CollectionName);
        SetupGetCollection<CardDocument>(mockDatabase, CardDocument.CollectionName);
        SetupGetCollection<DeckDocument>(mockDatabase, DeckDocument.CollectionName);
        SetupGetCollection<ReviewDocument>(mockDatabase, ReviewDocument.CollectionName);
        SetupGetCollection<UserDocument>(mockDatabase, UserDocument.CollectionName);
        SetupGetCollection<Verification>(mockDatabase, Verification.CollectionName);
        _ = builder.RegisterInstance(mockDatabase.Object);

        return builder.Build();
    });

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Constructor()
    {
        var target = new DataModule();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IDocumentDaoOfCardDocument()
    {
        var dao = Target.Value.Resolve<IDocumentDao<CardDocument>>();

        Assert.IsNotNull(dao);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IDocumentDaoOfDeckDocument()
    {
        var dao = Target.Value.Resolve<IDocumentDao<DeckDocument>>();

        Assert.IsNotNull(dao);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IDocumentDaoOfReviewDocument()
    {
        var dao = Target.Value.Resolve<IDocumentDao<ReviewDocument>>();

        Assert.IsNotNull(dao);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IDocumentDaoOfUserDocument()
    {
        var dao = Target.Value.Resolve<IDocumentDao<UserDocument>>();

        Assert.IsNotNull(dao);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    [DoNotParallelize]
    public async Task DataModule_Load_IMongoClient_ConfigurationChangeTracking()
    {
        var options = await GetDatabaseOptions().ConfigureAwait(false);

        await IntegrationTestUtilities.RunConfigurationChangeTrackingTest<DataModule, IMongoDatabase, DatabaseOptions>(
            options,
            InitializeConfigFile,
            ModifyConfigFile)
            .ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IMongoCollectionOfAuthenticationEvent()
    {
        var collection = Target.Value.Resolve<IMongoCollection<AuthenticationEvent>>();

        Assert.IsNotNull(collection);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IMongoCollectionOfCardDocument()
    {
        var collection = Target.Value.Resolve<IMongoCollection<CardDocument>>();

        Assert.IsNotNull(collection);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IMongoCollectionOfDeckDocument()
    {
        var collection = Target.Value.Resolve<IMongoCollection<DeckDocument>>();

        Assert.IsNotNull(collection);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IMongoCollectionOfReviewDocument()
    {
        var collection = Target.Value.Resolve<IMongoCollection<ReviewDocument>>();

        Assert.IsNotNull(collection);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IMongoCollectionOfUserDocument()
    {
        var collection = Target.Value.Resolve<IMongoCollection<UserDocument>>();

        Assert.IsNotNull(collection);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IMongoDatabase()
    {
        var database = Target.Value.Resolve<IMongoDatabase>();

        Assert.IsNotNull(database);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    [DoNotParallelize]
    public async Task DataModule_Load_IMongoDatabase_ConfigurationChangeTracking()
    {
        var options = await GetDatabaseOptions().ConfigureAwait(false);

        await IntegrationTestUtilities.RunConfigurationChangeTrackingTest<DataModule, IMongoDatabase, DatabaseOptions>(
            options,
            InitializeConfigFile,
            ModifyConfigFile)
            .ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IPasswordDao()
    {
        var dao = Target.Value.Resolve<IPasswordDao>();

        Assert.IsNotNull(dao);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IPasswordHasher()
    {
        var hasher = Target.Value.Resolve<IPasswordHasher>();

        Assert.IsNotNull(hasher);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IPersistentTokenDao()
    {
        var dao = Target.Value.Resolve<IPersistentTokenDao>();

        Assert.IsNotNull(dao);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IValidatorOfAuthenticationEvent()
    {
        var validator = Target.Value.Resolve<IValidator<AuthenticationEvent>>();

        Assert.IsNotNull(validator);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IValidatorOfCardDocument()
    {
        var validator = Target.Value.Resolve<IValidator<CardDocument>>();

        Assert.IsNotNull(validator);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IValidatorOfDeckDocument()
    {
        var validator = Target.Value.Resolve<IValidator<DeckDocument>>();

        Assert.IsNotNull(validator);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IValidatorOfReviewDocument()
    {
        var validator = Target.Value.Resolve<IValidator<ReviewDocument>>();

        Assert.IsNotNull(validator);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IValidatorOfUserDocument()
    {
        var validator = Target.Value.Resolve<IValidator<UserDocument>>();

        Assert.IsNotNull(validator);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void DataModule_Load_IVerificationDao()
    {
        var validator = Target.Value.Resolve<IVerificationDao>();

        Assert.IsNotNull(validator);
    }

    private static async Task<IOptions<DatabaseOptions>> GetDatabaseOptions()
    {
        var mockOptions = new Mock<IOptions<DatabaseOptions>>();
        var appSettings = await IntegrationTestUtilities.GetAppSettings(AppSettingsFilename).ConfigureAwait(false);
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new DatabaseOptions()
            {
                ConnectionString = appSettings.Hirameku.Data.DatabaseOptions.ConnectionString,
                DatabaseName = DatabaseName,
            });

        return mockOptions.Object;
    }

    private static Task InitializeConfigFile()
    {
        return IntegrationTestUtilities.ModifyAppSettingsFile(
            AppSettingsFilename,
            s => s.Hirameku.Data.DatabaseOptions.Database = DatabaseName);
    }

    private static Task ModifyConfigFile()
    {
        return IntegrationTestUtilities.ModifyAppSettingsFile(
            AppSettingsFilename,
            s => s.Hirameku.Data.DatabaseOptions.Database = DatabaseName + "2");
    }
}
