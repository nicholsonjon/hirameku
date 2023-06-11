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

namespace Hirameku.Contact.Tests;

using Hirameku.Common;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using IServiceCollectionExtensions = Hirameku.Contact.IServiceCollectionExtensions;

[TestClass]
public class IServiceCollectionExtensionsTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public void IServiceCollectionExtensions_AddContactConfiguration_ConfigurationIsNull_Throws()
    {
        var mockInstance = new Mock<IServiceCollection>();

        _ = IServiceCollectionExtensions.AddContactConfiguration(mockInstance.Object, null!);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public void IServiceCollectionExtensions_AddContactConfiguration_InstanceIsNull_Throws()
    {
        var mockConfiguration = new Mock<IConfiguration>();

        _ = IServiceCollectionExtensions.AddContactConfiguration(null!, mockConfiguration.Object);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddContactConfiguration_EmailerOptions()
    {
        var mockConfiguration = GetMockConfiguration();
        var target = new ServiceCollection();

        _ = target.AddContactConfiguration(mockConfiguration.Object);

        Assert.IsTrue(target.Any(s => s.ServiceType == typeof(IConfigureOptions<EmailerOptions>)));
        mockConfiguration.Verify(m => m.GetSection(EmailerOptions.ConfigurationSectionName));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddContactConfiguration_RecaptchaOptions()
    {
        var mockConfiguration = GetMockConfiguration();
        var target = new ServiceCollection();

        _ = target.AddContactConfiguration(mockConfiguration.Object);

        Assert.IsTrue(target.Any(s => s.ServiceType == typeof(IConfigureOptions<RecaptchaOptions>)));
        mockConfiguration.Verify(m => m.GetSection(RecaptchaOptions.ConfigurationSectionName));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddContactConfiguration_VerificationOptions()
    {
        var mockConfiguration = GetMockConfiguration();
        var target = new ServiceCollection();

        _ = target.AddContactConfiguration(mockConfiguration.Object);

        Assert.IsTrue(target.Any(s => s.ServiceType == typeof(IConfigureOptions<VerificationOptions>)));
        mockConfiguration.Verify(m => m.GetSection(VerificationOptions.ConfigurationSectionName));
    }

    private static Mock<IConfiguration> GetMockConfiguration()
    {
        var configurationSection = Mock.Of<IConfigurationSection>();
        var mockConfiguration = new Mock<IConfiguration>();
        _ = mockConfiguration.Setup(m => m.GetSection(EmailerOptions.ConfigurationSectionName))
            .Returns(configurationSection);
        _ = mockConfiguration.Setup(m => m.GetSection(RecaptchaOptions.ConfigurationSectionName))
            .Returns(configurationSection);
        _ = mockConfiguration.Setup(m => m.GetSection(VerificationOptions.ConfigurationSectionName))
            .Returns(configurationSection);

        return mockConfiguration;
    }
}
