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
using Hirameku.Common.Properties;
using Hirameku.Data;
using NLog;
using System.Globalization;
using System.Threading;

public class CachedValueDao : ICachedValueDao
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public CachedValueDao(ICacheClient cacheClient, IDocumentDao<UserDocument> userDao)
    {
        this.CacheClient = cacheClient;
        this.UserDao = userDao;
    }

    private ICacheClient CacheClient { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    public async Task<UserStatus> GetUserStatus(string userId, CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { userId, cancellationToken } });

        var key = CacheSubkeys.UserStatusSubkey + userId;
        var cachedValue = await this.CacheClient.GetValue(key, cancellationToken).ConfigureAwait(false);

        if (!Enum.TryParse<UserStatus>(cachedValue, out var userStatus))
        {
            var user = await this.UserDao.Fetch(userId, cancellationToken).ConfigureAwait(false);

            if (user != null)
            {
                userStatus = user.UserStatus;

                await this.CacheClient.SetValue(key, userStatus.ToString(), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Exceptions.UserIdDoesNotExist,
                    userId);

                throw new UserDoesNotExistException(message);
            }
        }

        Log.Trace("Exiting method", data: new { returnValue = userStatus });

        return userStatus;
    }

    public async Task SetUserStatus(string userId, UserStatus userStatus)
    {
        Log.Trace("Entering method", data: new { parameters = new { userId, userStatus } });

        var key = CacheSubkeys.UserStatusSubkey + userId;

        // cancellation is intentionally not supported because we don't want to update the cache and then cancel before
        // we update the database
        await this.CacheClient.SetValue(key, userStatus.ToString()).ConfigureAwait(false);
        await this.UserDao.Update(userId, u => u.UserStatus, userStatus).ConfigureAwait(false);

        Log.Trace("Exiting method", data: default(object));
    }
}
