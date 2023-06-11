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

using Hirameku.Data;
using Microsoft.Extensions.Options;
using Moq;

namespace Hirameku.Common.Service.Tests;

[TestClass]
public class PersistentTokenIssuerTests
{
    private const string ClientId = nameof(ClientId);
    private const int ClientTokenLength = 32;
    private const string UserId = nameof(UserId);
    private static readonly DateTime ExpirationDate = DateTime.UtcNow.AddDays(365);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PersistentTokenIssuer_Constructor()
    {
        var target = GetTarget();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task PersistentTokenIssuer_Issue()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var target = GetTarget(cancellationToken: cancellationToken);

        var token = await target.Issue(UserId, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(ClientTokenLength, Convert.FromBase64String(token.ClientToken).Length);
        Assert.AreEqual(ClientId, token.ClientId);
        Assert.AreEqual(ExpirationDate, token.ExpirationDate);
        Assert.AreEqual(UserId, token.UserId);
    }

    private static Mock<IPersistentTokenDao> GetMockDao(CancellationToken cancellationToken = default)
    {
        var mockDao = new Mock<IPersistentTokenDao>();
        _ = mockDao.Setup(m => m.SavePersistentToken(UserId, ClientId, It.IsAny<string>(), cancellationToken))
            .Callback<string, string, string, CancellationToken>(
                (uid, cid, clt, cat) => Assert.AreEqual(ClientTokenLength, Convert.FromBase64String(clt).Length))
            .ReturnsAsync(ExpirationDate);

        return mockDao;
    }

    private static Mock<IOptions<PersistentTokenOptions>> GetMockOptions()
    {
        var mockOptions = new Mock<IOptions<PersistentTokenOptions>>();
        _ = mockOptions.Setup(m => m.Value)
            .Returns(new PersistentTokenOptions() { ClientTokenLength = ClientTokenLength });

        return mockOptions;
    }

    private static Mock<IUniqueIdGenerator> GetMockUniqueIdGenerator()
    {
        var mockUniqueIdGenerator = new Mock<IUniqueIdGenerator>();
        _ = mockUniqueIdGenerator.Setup(m => m.GenerateUniqueId())
            .Returns(ClientId);

        return mockUniqueIdGenerator;
    }

    private static PersistentTokenIssuer GetTarget(
        Mock<IPersistentTokenDao>? mockDao = default,
        Mock<IOptions<PersistentTokenOptions>>? mockOptions = default,
        Mock<IUniqueIdGenerator>? mockUniqueIdGenerator = default,
        CancellationToken cancellationToken = default)
    {
        mockDao ??= GetMockDao(cancellationToken);
        mockOptions ??= GetMockOptions();
        mockUniqueIdGenerator ??= GetMockUniqueIdGenerator();

        return new PersistentTokenIssuer(mockDao.Object, mockOptions.Object, mockUniqueIdGenerator.Object);
    }
}
