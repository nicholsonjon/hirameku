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

namespace Hirameku.Registration.Tests;

using Autofac;
using AutoMapper;
using FluentValidation;
using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

[TestClass]
public class RegistrationModuleTests
{
    private static readonly Lazy<IContainer> Target = new(() =>
    {
        var builder = new ContainerBuilder();
        _ = builder.RegisterModule<RegistrationModule>();

        // we mock the configuration-based dependencies because these come from the IServiceCollection and are not
        // directly managed by the IoC container
        _ = builder.Register(_ => Mock.Of<IOptions<CacheOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<DatabaseOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<EmailerOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<PasswordOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<PasswordValidatorOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<PersistentTokenOptions>>());
        _ = builder.Register(_ => Mock.Of<IOptions<RecaptchaOptions>>());
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

        SetupGetCollection<UserDocument>(mockDatabase, UserDocument.CollectionName);
        SetupGetCollection<Verification>(mockDatabase, Verification.CollectionName);
        _ = builder.RegisterInstance(mockDatabase.Object);

        var mockCacheClientFactory = new Mock<ICacheClientFactory>();
        _ = mockCacheClientFactory.Setup(m => m.CreateClient())
            .Returns(Mock.Of<ICacheClient>());
        _ = builder.RegisterInstance(mockCacheClientFactory.Object);

        return builder.Build();
    });

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegistrationModule_Constructor()
    {
        var target = new RegistrationModule();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegistrationModule_Load_ExceptionsProfile()
    {
        var profiles = Target.Value.Resolve<IEnumerable<Profile>>();

        Assert.IsTrue(profiles.OfType<ExceptionsProfile>().Any());
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegistrationModule_Load_IMapper()
    {
        var mapper = Target.Value.Resolve<IMapper>();

        Assert.IsNotNull(mapper);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegistrationModule_Load_IRegistrationProvider()
    {
        var provider = Target.Value.Resolve<IRegistrationProvider>();

        Assert.IsNotNull(provider);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegistrationModule_Load_IValidatorOfRegisterModel()
    {
        var validator = Target.Value.Resolve<IValidator<RegisterModel>>();

        Assert.IsNotNull(validator);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegistrationModule_Load_IValidatorOfResendVerificationEmailModel()
    {
        var validator = Target.Value.Resolve<IValidator<ResendVerificationEmailModel>>();

        Assert.IsNotNull(validator);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void RegistrationModule_Load_RegistrationProfile()
    {
        var profiles = Target.Value.Resolve<IEnumerable<Profile>>();

        Assert.IsTrue(profiles.OfType<RegistrationProfile>().Any());
    }
}
