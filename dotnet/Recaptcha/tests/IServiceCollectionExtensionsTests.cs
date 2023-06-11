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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

[TestClass]
public class IServiceCollectionExtensionsTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public void IServiceCollectionExtensions_AddRecaptchaConfiguration_ConfigurationIsNull_Throws()
    {
        var mockInstance = new Mock<IServiceCollection>();

        _ = IServiceCollectionExtensions.AddRecaptchaConfiguration(mockInstance.Object, null!);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddRecaptchaConfiguration_HttpClient()
    {
        var mockConfiguration = GetMockConfiguration();
        var target = new ServiceCollection();

        _ = target.AddRecaptchaConfiguration(mockConfiguration.Object);

        Assert.IsTrue(target.Any(s => s.ServiceType == typeof(HttpClient)));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddRecaptchaConfiguration_HttpClient_BackoffRetryPolicy()
    {
        // TODO: https://stackoverflow.com/questions/61254623/unit-test-httpclient-with-polly
        Assert.Inconclusive();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public void IServiceCollectionExtensions_AddRecaptchaConfiguration_InstanceIsNull_Throws()
    {
        var mockConfiguration = new Mock<IConfiguration>();

        _ = IServiceCollectionExtensions.AddRecaptchaConfiguration(null!, mockConfiguration.Object);

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void IServiceCollectionExtensions_AddRecaptchaConfiguration_RecaptchaOptions()
    {
        var mockConfiguration = GetMockConfiguration();
        var target = new ServiceCollection();

        _ = target.AddRecaptchaConfiguration(mockConfiguration.Object);

        Assert.IsTrue(target.Any(s => s.ServiceType == typeof(IConfigureOptions<RecaptchaOptions>)));
        mockConfiguration.Verify(m => m.GetSection(RecaptchaOptions.ConfigurationSectionName));
    }

    private static Mock<IConfiguration> GetMockConfiguration()
    {
        var configurationSection = Mock.Of<IConfigurationSection>();
        var mockConfiguration = new Mock<IConfiguration>();
        _ = mockConfiguration.Setup(m => m.GetSection(RecaptchaOptions.ConfigurationSectionName))
            .Returns(configurationSection);

        return mockConfiguration;
    }
}
