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
using System.Threading.Tasks;

public class CacheClientFactory : ICacheClientFactory
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private bool disposed;

    public CacheClientFactory(IOptions<CacheOptions> options)
    {
        this.Options = options;
    }

    private static IConnectionMultiplexer? Connection { get; set; }

    private IOptions<CacheOptions> Options { get; }

    public ICacheClient CreateClient()
    {
        Log.Trace("Exiting method", data: default(object));

        if (this.disposed)
        {
            throw new ObjectDisposedException(this.GetType().FullName);
        }

        var options = this.Options.Value;
        var client = new CacheClient(GetDatabase(options.ConnectionString, options.DatabaseNumber), this.Options);

        Log.Trace("Exiting method", data: new { returnValue = client });

        return client;
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await this.DisposeAsyncCore().ConfigureAwait(false);
        this.Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        Log.Trace("Entering method", data: new { parameters = new { disposing } });

        if (!this.disposed)
        {
            if (disposing)
            {
                Connection?.Dispose();
            }

            Connection = null;

            this.disposed = true;
        }

        Log.Trace("Exiting method", data: default(object));
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        Log.Trace("Exiting method", data: default(object));

        if (Connection != null)
        {
            await Connection.DisposeAsync().ConfigureAwait(false);
        }

        Connection = null;

        Log.Trace("Exiting method", data: default(object));
    }

    private static IDatabase GetDatabase(string connectionString, int databaseNumber)
    {
        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new
                {
                    connectionString = "REDACTED",
                    databaseNumber,
                },
            });

        var configurationOptions = ConfigurationOptions.Parse(connectionString);
        var password = configurationOptions.Password;
        configurationOptions.Password = null;

        Log.Debug("Connecting to cache server", data: new { connectionString = configurationOptions.ToString() });

        configurationOptions.Password = password;

        Connection ??= ConnectionMultiplexer.Connect(configurationOptions);

        Log.Debug("Getting database proxy", data: new { databaseNumber });

        var database = Connection.GetDatabase(databaseNumber);

        Log.Trace("Exiting method", data: new { returnValue = database });

        return database;
    }
}
