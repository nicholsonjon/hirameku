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

namespace Hirameku.Caching;

using Hirameku.Common;
using Microsoft.Extensions.Options;
using NLog;
using StackExchange.Redis;

public class CacheClient : ICacheClient
{
    // we use a placeholder value for cooldown keys because the presence (or absence) of the key is all we care about
    private const string CooldownPlaceholderValue = "1";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public CacheClient(IDatabase database, IOptions<CacheOptions> options)
    {
        this.Database = database;
        this.Options = options;
    }

    private IDatabase Database { get; }

    private IOptions<CacheOptions> Options { get; }

    public Task<CooldownStatus> GetCooldownStatus(string key, CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { key, cancellationToken } });

        var status = this.GetCooldownStatus(key, this.Options.Value.CooldownTimeToLive, cancellationToken);

        Log.Trace("Exiting method", data: new { returnValue = status });

        return status;
    }

    public async Task<CooldownStatus> GetCooldownStatus(
        string key,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { key, timeToLive, cancellationToken } });

        var database = this.Database;
        var value = await database.StringSetAndGetAsync(key, CooldownPlaceholderValue, timeToLive, true)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        Log.Debug("Cooldown set", data: new { key, timeToLive, value });

        var expireTime = await database.KeyExpireTimeAsync(key)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        Log.Debug("Cooldown expiry set", data: new { key, expireTime });

        var status = new CooldownStatus(expireTime, value == CooldownPlaceholderValue, timeToLive);

        Log.Trace("Exiting method", data: new { returnValue = status });

        return status;
    }

    public async Task<string?> GetValue(string key, CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { key, cancellationToken } });

        var value = await this.Database.StringGetAsync(key)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        Log.Debug("Got value", data: new { key, value });
        Log.Trace("Exiting method", data: new { returnValue = value });

        return value;
    }

    public Task<long> IncrementCounter(string key, CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { key, cancellationToken } });

        var counter = this.IncrementCounter(key, this.Options.Value.CounterTimeToLive, cancellationToken);

        Log.Trace("Exiting method", data: new { returnValue = counter });

        return counter;
    }

    public async Task<long> IncrementCounter(
        string key,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { key, timeToLive, cancellationToken } });

        var database = this.Database;
        var counter = await database.StringIncrementAsync(key)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        Log.Debug("Counter incremented", data: new { key, counter });

        cancellationToken.ThrowIfCancellationRequested();

        var expireTime = await database.KeyExpireAsync(key, timeToLive)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        Log.Debug("Counter expiry set", data: new { key, expireTime });
        Log.Trace("Exiting method", data: new { returnValue = counter });

        return counter;
    }

    public Task SetValue(string key, string value, CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { key, cancellationToken } });

        var task = this.SetValue(key, value, this.Options.Value.ValueTimeToLive, cancellationToken);

        Log.Trace("Exiting method", data: new { returnValue = task });

        return task;
    }

    public async Task SetValue(
        string key,
        string value,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { key, timeToLive, cancellationToken } });

        var result = await this.Database.StringSetAsync(key, value, timeToLive)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        Log.Debug("Value set", data: new { key, value, result });
        Log.Trace("Exiting method", data: default(object));
    }
}
