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
using Hirameku.Authentication;
using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace Hirameku.IdentityService.Tests;

[TestClass]
public class IdentityServiceModuleTests
{
    private static readonly Lazy<IContainer> Target = new(() =>
    {
        var builder = new ContainerBuilder();
        _ = builder.RegisterModule<IdentityServiceModule>();

        // we mock the configuration-based dependencies because these come from the IServiceCollection and are not
        // directly managed by the IoC container
        _ = builder.Register(_ => Mock.Of<IOptions<AuthenticationOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<CacheOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<DatabaseOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<EmailerOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<PasswordOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<PasswordValidatorOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<PersistentTokenOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<RecaptchaOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<SecurityTokenOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<VerificationOptions>>());
        _ = builder.Register(_ => Mock.Of<HttpClient>());

        // IMongoDatabase and ICacheClientFactory must be mocked because their registration delegates take out
        // connections to databases
        var mockDatabase = new Mock<IMongoDatabase>();

        void SetupGetCollection<TDocument>(Mock<IMongoDatabase> mock, string collectionName)
        {
            _ = mock.Setup(m => m.GetCollection<TDocument>(collectionName, default))
                .Returns(Mock.Of<IMongoCollection<TDocument>>());
        }

        SetupGetCollection<AuthenticationEvent>(mockDatabase, AuthenticationEvent.CollectionName);
        SetupGetCollection<UserDocument>(mockDatabase, UserDocument.CollectionName);
        SetupGetCollection<Verification>(mockDatabase, Verification.CollectionName);
        _ = builder.RegisterInstance(mockDatabase.Object);

        var mockCacheClientFactory = new Mock<ICacheClientFactory>();
        _ = mockCacheClientFactory.Setup(m => m.CreateClient())
            .Returns(Mock.Of<ICacheClient>());
        _ = builder.RegisterInstance(mockCacheClientFactory.Object);

        // register the controllers, so we can resolve them as a means of testing the dependency resolution
        _ = builder.RegisterType<HttpContextAccessor>().As<IHttpContextAccessor>();
        _ = builder.RegisterType<AuthenticationController>();
        _ = builder.RegisterType<RegistrationController>();
        _ = builder.RegisterType<UserController>();

        return builder.Build();
    });

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IdentityServiceModule_Constructor()
    {
        var target = new IdentityServiceModule();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IdentityServiceModule_Load_AuthenticationController()
    {
        var controller = Target.Value.Resolve<AuthenticationController>();

        Assert.IsNotNull(controller);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IdentityServiceModule_Load_RegistrationController()
    {
        var controller = Target.Value.Resolve<RegistrationController>();

        Assert.IsNotNull(controller);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IdentityServiceModule_Load_UserController()
    {
        var controller = Target.Value.Resolve<UserController>();

        Assert.IsNotNull(controller);
    }
}
