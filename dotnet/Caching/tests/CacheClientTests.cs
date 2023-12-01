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

using Hirameku.Caching;
using Hirameku.Common;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;

[TestClass]
public class CacheClientTests
{
    private const string ConnectionString = nameof(ConnectionString);
    private const string CooldownKey = CacheSubkeys.EmailCooldownSubkey + EmailAddress;
    private const int DatabaseNumber = -1;
    private const string EmailAddress = nameof(EmailAddress);
    private const string SignInKey = CacheSubkeys.SignInSubkey + UserId;
    private const string UserId = nameof(UserId);
    private const string ValueKey = CacheSubkeys.UserStatusSubkey + UserId;
    private static readonly TimeSpan OperationTimeout = new(0, 0, 5);
    private static readonly TimeSpan TimeToLive = new(0, 5, 0);

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void CacheClient_Constructor()
    {
        var target = new CacheClient(
            new Mock<IDatabase>().Object,
            new Mock<IOptions<CacheOptions>>().Object);

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [DataRow(null, false, DisplayName = nameof(CacheClient_GetCooldownStatus) + "(null, false)")]
    [DataRow("", false, DisplayName = nameof(CacheClient_GetCooldownStatus) + "(string.Empty, false)")]
    [DataRow("0", false, DisplayName = nameof(CacheClient_GetCooldownStatus) + "(\"0\", false)")]
    [DataRow("1", true, DisplayName = nameof(CacheClient_GetCooldownStatus) + "(\"1\", true)")]
    public async Task CacheClient_GetCooldownStatus(string value, bool isOnCooldown)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockDatabase = new Mock<IDatabase>();
        const string CooldownSentinelValue = "1";
        var expireTime = DateTime.UtcNow + TimeToLive;
        _ = mockDatabase.Setup(
            m => m.StringSetAndGetAsync(
                CooldownKey,
                CooldownSentinelValue,
                TimeToLive,
                true,
                When.Always,
                CommandFlags.None))
            .ReturnsAsync(new RedisValue(value));
        _ = mockDatabase.Setup(m => m.KeyExpireTimeAsync(CooldownKey, CommandFlags.None))
            .ReturnsAsync(expireTime);
        var target = GetTarget(mockDatabase);

        var actual = await target.GetCooldownStatus(CooldownKey, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expireTime, actual.ExpireTime);
        Assert.AreEqual(isOnCooldown, actual.IsOnCooldown);
        Assert.AreEqual(TimeToLive, actual.TimeToLive);
        mockDatabase.VerifyAll();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Test code")]
    public async Task CacheClient_GetCooldownStatus_CancellationRequestedAtExpire()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockDatabase = new Mock<IDatabase>();
        const string CooldownSentinelValue = "1";
        Expression<Func<IDatabase, Task<RedisValue>>> setAndGetSetup = m => m.StringSetAndGetAsync(
            CooldownKey,
            CooldownSentinelValue,
            TimeToLive,
            true,
            When.Always,
            CommandFlags.None);
        _ = mockDatabase.Setup(setAndGetSetup)
            .ReturnsAsync(new RedisValue(CooldownSentinelValue));
        Expression<Func<IDatabase, Task<DateTime?>>> keyExpireTimeSetup =
            m => m.KeyExpireTimeAsync(CooldownKey, CommandFlags.None);
        _ = mockDatabase.Setup(keyExpireTimeSetup)
            .Callback<RedisKey, CommandFlags>((k, cf) => cancellationTokenSource.Cancel())
            .ReturnsAsync(DateTime.UtcNow + TimeToLive);
        var target = GetTarget(mockDatabase);

        try
        {
            _ = await target.GetCooldownStatus(CooldownKey, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            mockDatabase.Verify(setAndGetSetup, Times.Once);
            mockDatabase.Verify(keyExpireTimeSetup, Times.Once);
        }
        catch (Exception ex)
        {
            Assert.Fail("Unexpected exception occurred: {0}", ex);
        }
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Test code")]
    public async Task CacheClient_GetCooldownStatus_CancellationRequestedAtSetAndGet()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockDatabase = new Mock<IDatabase>();
        const string CooldownSentinelValue = "1";
        Expression<Func<IDatabase, Task<RedisValue>>> setAndGetSetup = m => m.StringSetAndGetAsync(
            CooldownKey,
            CooldownSentinelValue,
            TimeToLive,
            true,
            When.Always,
            CommandFlags.None);
        _ = mockDatabase.Setup(setAndGetSetup)
            .Callback<RedisKey, RedisValue, TimeSpan?, bool, When, CommandFlags>(
                (k, v, e, ttl, w, cf) => cancellationTokenSource.Cancel())
            .ReturnsAsync(new RedisValue(CooldownSentinelValue));
        Expression<Func<IDatabase, Task<DateTime?>>> keyExpireTimeSetup =
            m => m.KeyExpireTimeAsync(CooldownKey, CommandFlags.None);
        _ = mockDatabase.Setup(keyExpireTimeSetup)
            .ReturnsAsync(DateTime.UtcNow + TimeToLive);
        var target = GetTarget(mockDatabase);

        try
        {
            _ = await target.GetCooldownStatus(CooldownKey, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            mockDatabase.Verify(setAndGetSetup, Times.Once);
            mockDatabase.Verify(keyExpireTimeSetup, Times.Never);
        }
        catch (Exception ex)
        {
            Assert.Fail("Unexpected exception occurred: {0}", ex);
        }
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task CacheClient_GetValue()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await RunGetValueTest(cancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(OperationCanceledException))]
    public async Task CacheClient_GetValue_CancellationRequested()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        await RunGetValueTest(cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(OperationCanceledException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task CacheClient_IncrementCounter()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockDatabase = new Mock<IDatabase>();
        const long Counter = 1L;
        const long Increment = 1L;
        _ = mockDatabase.Setup(m => m.StringIncrementAsync(SignInKey, Increment, CommandFlags.None))
            .ReturnsAsync(Counter + Increment);
        _ = mockDatabase.Setup(m => m.KeyExpireAsync(SignInKey, TimeToLive, ExpireWhen.Always, CommandFlags.None))
            .ReturnsAsync(true);
        var target = GetTarget(mockDatabase);

        var actual = await target.IncrementCounter(SignInKey, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(Counter + Increment, actual);
        mockDatabase.VerifyAll();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Test code")]
    public async Task CacheClient_IncrementCounter_CancellationRequestedAtExpire()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockDatabase = new Mock<IDatabase>();
        Expression<Func<IDatabase, Task<long>>> stringIncrementSetup =
            m => m.StringIncrementAsync(SignInKey, 1, CommandFlags.None);
        _ = mockDatabase.Setup(stringIncrementSetup)
            .ReturnsAsync(default(long));
        Expression<Func<IDatabase, Task<bool>>> keyExpireSetup =
            m => m.KeyExpireAsync(SignInKey, TimeToLive, ExpireWhen.Always, CommandFlags.None);
        _ = mockDatabase.Setup(keyExpireSetup)
            .Callback<RedisKey, TimeSpan?, ExpireWhen, CommandFlags>(
                (k, e, ew, cf) => cancellationTokenSource.Cancel());
        var target = GetTarget(mockDatabase);

        try
        {
            _ = await target.IncrementCounter(SignInKey, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            mockDatabase.Verify(stringIncrementSetup, Times.Once);
            mockDatabase.Verify(keyExpireSetup, Times.Once);
        }
        catch (Exception ex)
        {
            Assert.Fail("Unexpected exception occurred: {0}", ex);
        }
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Test code")]
    public async Task CacheClient_IncrementCounter_CancellationRequestedAtIncrement()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mockDatabase = new Mock<IDatabase>();
        Expression<Func<IDatabase, Task<long>>> stringIncrementSetup =
            m => m.StringIncrementAsync(SignInKey, 1, CommandFlags.None);
        _ = mockDatabase.Setup(stringIncrementSetup)
            .Callback<RedisKey, long, CommandFlags>((k, v, cf) => cancellationTokenSource.Cancel())
            .ReturnsAsync(default(long));
        Expression<Func<IDatabase, Task<bool>>> keyExpireSetup =
            m => m.KeyExpireAsync(SignInKey, TimeToLive, ExpireWhen.Always, CommandFlags.None);
        _ = mockDatabase.Setup(keyExpireSetup);
        var target = GetTarget(mockDatabase);

        try
        {
            _ = await target.IncrementCounter(SignInKey, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            mockDatabase.Verify(stringIncrementSetup, Times.Once);
            mockDatabase.Verify(keyExpireSetup, Times.Never);
        }
        catch (Exception ex)
        {
            Assert.Fail("Unexpected exception occurred: {0}", ex);
        }
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public async Task CacheClient_SetValue()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await RunSetValueTest(cancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(OperationCanceledException))]
    public async Task CacheClient_SetValue_CancellationRequested()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        await RunSetValueTest(cancellationToken).ConfigureAwait(false);

        Assert.Fail(nameof(OperationCanceledException) + " expected");
    }

    private static Mock<IOptions<CacheOptions>> GetMockOptions()
    {
        var mockOptions = new Mock<IOptions<CacheOptions>>();
        mockOptions.Setup(m => m.Value)
            .Returns(new CacheOptions()
            {
                ConnectionString = ConnectionString,
                CooldownTimeToLive = TimeToLive,
                CounterTimeToLive = TimeToLive,
                DatabaseNumber = DatabaseNumber,
                OperationTimeout = OperationTimeout,
                ValueTimeToLive = TimeToLive,
            })
            .Verifiable();

        return mockOptions;
    }

    private static CacheClient GetTarget(
        Mock<IDatabase> mockDatabase,
        Mock<IOptions<CacheOptions>>? mockOptions = default)
    {
        return new CacheClient(mockDatabase.Object, (mockOptions ?? GetMockOptions()).Object);
    }

    private static async Task RunGetValueTest(CancellationToken cancellationToken = default)
    {
        var mockDatabase = new Mock<IDatabase>();
        var expected = UserStatus.OK.ToString();
        _ = mockDatabase.Setup(m => m.StringGetAsync(ValueKey, default))
            .ReturnsAsync(expected);
        var target = GetTarget(mockDatabase);

        var actual = await target.GetValue(ValueKey, cancellationToken).ConfigureAwait(false);

        Assert.AreEqual(expected, actual);
        mockDatabase.VerifyAll();
    }

    private static async Task RunSetValueTest(CancellationToken cancellationToken = default)
    {
        var mockDatabase = new Mock<IDatabase>();
        var value = UserStatus.OK.ToString();
        _ = mockDatabase.Setup(
            m => m.StringSetAsync(ValueKey, value, TimeToLive, false, When.Always, CommandFlags.None))
            .ReturnsAsync(true);
        var target = GetTarget(mockDatabase);

        await target.SetValue(ValueKey, value, cancellationToken).ConfigureAwait(false);

        mockDatabase.VerifyAll();
    }
}
