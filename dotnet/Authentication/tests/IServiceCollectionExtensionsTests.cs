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

namespace Hirameku.Authentication.Tests;

using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using IServiceCollectionExtensions = Hirameku.Authentication.IServiceCollectionExtensions;

[TestClass]
public class IServiceCollectionExtensionsTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddAuthenticationConfiguration_AuthenticationOptions()
    {
        var mockConfiguration = GetMockConfiguration();
        var target = new ServiceCollection();

        _ = target.AddAuthenticationConfiguration(mockConfiguration.Object);

        Assert.IsTrue(target.Any(s => s.ServiceType == typeof(IConfigureOptions<AuthenticationOptions>)));
        mockConfiguration.Verify(m => m.GetSection(AuthenticationOptions.ConfigurationSectionName));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddAuthenticationConfiguration_CacheOptions()
    {
        var mockConfiguration = GetMockConfiguration();
        var target = new ServiceCollection();

        _ = target.AddAuthenticationConfiguration(mockConfiguration.Object);

        Assert.IsTrue(target.Any(s => s.ServiceType == typeof(IConfigureOptions<CacheOptions>)));
        mockConfiguration.Verify(m => m.GetSection(CacheOptions.ConfigurationSectionName));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public void IServiceCollectionExtensions_AddAuthenticationConfiguration_ConfigurationIsNull_Throws()
    {
        var mockInstance = new Mock<IServiceCollection>();

        _ = IServiceCollectionExtensions.AddAuthenticationConfiguration(mockInstance.Object, null!);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddAuthenticationConfiguration_DatabaseOptions()
    {
        var mockConfiguration = GetMockConfiguration();
        var target = new ServiceCollection();

        _ = target.AddAuthenticationConfiguration(mockConfiguration.Object);

        Assert.IsTrue(target.Any(s => s.ServiceType == typeof(IConfigureOptions<DatabaseOptions>)));
        mockConfiguration.Verify(m => m.GetSection(DatabaseOptions.ConfigurationSectionName));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddAuthenticationConfiguration_EmailerOptions()
    {
        var mockConfiguration = GetMockConfiguration();
        var target = new ServiceCollection();

        _ = target.AddAuthenticationConfiguration(mockConfiguration.Object);

        Assert.IsTrue(target.Any(s => s.ServiceType == typeof(IConfigureOptions<EmailerOptions>)));
        mockConfiguration.Verify(m => m.GetSection(EmailerOptions.ConfigurationSectionName));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public void IServiceCollectionExtensions_AddAuthenticationConfiguration_InstanceIsNull_Throws()
    {
        var mockConfiguration = new Mock<IConfiguration>();

        _ = IServiceCollectionExtensions.AddAuthenticationConfiguration(null!, mockConfiguration.Object);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddAuthenticationConfiguration_PasswordOptions()
    {
        var mockConfiguration = GetMockConfiguration();
        var target = new ServiceCollection();

        _ = target.AddAuthenticationConfiguration(mockConfiguration.Object);

        Assert.IsTrue(target.Any(s => s.ServiceType == typeof(IConfigureOptions<PasswordOptions>)));
        mockConfiguration.Verify(m => m.GetSection(PasswordOptions.ConfigurationSectionName));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddAuthenticationConfiguration_PasswordValidatorOptions()
    {
        var mockConfiguration = GetMockConfiguration();
        var target = new ServiceCollection();

        _ = target.AddAuthenticationConfiguration(mockConfiguration.Object);

        Assert.IsTrue(target.Any(s => s.ServiceType == typeof(IConfigureOptions<PasswordValidatorOptions>)));
        mockConfiguration.Verify(m => m.GetSection(PasswordValidatorOptions.ConfigurationSectionName));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddAuthenticationConfiguration_PersistentTokenOptions()
    {
        var mockConfiguration = GetMockConfiguration();
        var target = new ServiceCollection();

        _ = target.AddAuthenticationConfiguration(mockConfiguration.Object);

        Assert.IsTrue(target.Any(s => s.ServiceType == typeof(IConfigureOptions<PersistentTokenOptions>)));
        mockConfiguration.Verify(m => m.GetSection(PersistentTokenOptions.ConfigurationSectionName));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddAuthenticationConfiguration_VerificationOptions()
    {
        var mockConfiguration = GetMockConfiguration();
        var target = new ServiceCollection();

        _ = target.AddAuthenticationConfiguration(mockConfiguration.Object);

        Assert.IsTrue(target.Any(s => s.ServiceType == typeof(IConfigureOptions<VerificationOptions>)));
        mockConfiguration.Verify(m => m.GetSection(VerificationOptions.ConfigurationSectionName));
    }

    private static Mock<IConfiguration> GetMockConfiguration()
    {
        var configurationSection = Mock.Of<IConfigurationSection>();
        var mockConfiguration = new Mock<IConfiguration>();
        _ = mockConfiguration.Setup(m => m.GetSection(AuthenticationOptions.ConfigurationSectionName))
            .Returns(configurationSection);
        _ = mockConfiguration.Setup(m => m.GetSection(CacheOptions.ConfigurationSectionName))
            .Returns(configurationSection);
        _ = mockConfiguration.Setup(m => m.GetSection(DatabaseOptions.ConfigurationSectionName))
            .Returns(configurationSection);
        _ = mockConfiguration.Setup(m => m.GetSection(EmailerOptions.ConfigurationSectionName))
            .Returns(configurationSection);
        _ = mockConfiguration.Setup(m => m.GetSection(PasswordOptions.ConfigurationSectionName))
            .Returns(configurationSection);
        _ = mockConfiguration.Setup(m => m.GetSection(PasswordValidatorOptions.ConfigurationSectionName))
            .Returns(configurationSection);
        _ = mockConfiguration.Setup(m => m.GetSection(PersistentTokenOptions.ConfigurationSectionName))
            .Returns(configurationSection);
        _ = mockConfiguration.Setup(m => m.GetSection(RecaptchaOptions.ConfigurationSectionName))
            .Returns(configurationSection);
        _ = mockConfiguration.Setup(m => m.GetSection(SecurityTokenOptions.ConfigurationSectionName))
            .Returns(configurationSection);
        _ = mockConfiguration.Setup(m => m.GetSection(VerificationOptions.ConfigurationSectionName))
            .Returns(configurationSection);

        return mockConfiguration;
    }
}
