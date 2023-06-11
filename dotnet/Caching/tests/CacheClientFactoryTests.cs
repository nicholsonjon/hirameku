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

using Microsoft.Extensions.Options;
using Moq;

[TestClass]
public class CacheClientFactoryTests
{
    private const string ConnectionString = "localhost:6379";
    private const int DatabaseNumber = 0;

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CacheClientFactory_Constructor()
    {
        using var target = new CacheClientFactory(Mock.Of<IOptions<CacheOptions>>());

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public void CacheClientFactory_CreateClient()
    {
        using var target = GetTarget();

        var client = target.CreateClient();

        Assert.IsNotNull(client);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void CacheClientFactory_CreateClient_ObjectDisposed()
    {
        var target = GetTarget();

        target.Dispose();
        _ = target.CreateClient();

        Assert.Fail(nameof(ObjectDisposedException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CacheClientFactory_Dispose()
    {
        var target = GetTarget();

        target.Dispose();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task CacheClientFactory_DisposeAsync()
    {
        var target = GetTarget();

        await target.DisposeAsync().ConfigureAwait(false);
    }

    private static Mock<IOptions<CacheOptions>> GetMockOptions()
    {
        var mockOptions = new Mock<IOptions<CacheOptions>>();
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new CacheOptions()
            {
                ConnectionString = ConnectionString,
                DatabaseNumber = DatabaseNumber,
            });

        return mockOptions;
    }

    private static CacheClientFactory GetTarget()
    {
        var mockOptions = GetMockOptions();

        return new CacheClientFactory(mockOptions.Object);
    }
}
