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

namespace Hirameku.Authentication;

using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using NLog;
using NLog.Fluent;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using CommonExceptions = Hirameku.Common.Properties.Exceptions;
using ServiceExceptions = Hirameku.Common.Service.Properties.Exceptions;

public static class IDocumentDaoOfUserDocumentExtensions
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static async Task<UserDocument> GetUserById(
        this IDocumentDao<UserDocument> instance,
        string userId,
        CancellationToken cancellationToken = default)
    {
        Log.Debug("Fetching User document", data: new { parameters = new { instance, userId, cancellationToken } });

        ArgumentNullException.ThrowIfNull(instance);

        var user = await GetUser(instance, u => u.Id == userId, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                CompositeFormat.Parse(CommonExceptions.UserIdDoesNotExist).Format,
                userId);

            throw new UserDoesNotExistException(message);
        }

        Log.Debug("User fetched", data: new { id = userId });

        return user;
    }

    public static async Task<UserDocument> GetUserByUserName(
        this IDocumentDao<UserDocument> instance,
        string userName,
        CancellationToken cancellationToken = default)
    {
        Log.Debug("Fetching User document", data: new { parameters = new { instance, userName, cancellationToken } });

        ArgumentNullException.ThrowIfNull(instance);

        var user = await GetUser(instance, u => u.UserName == userName, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                CompositeFormat.Parse(CommonExceptions.UserNameDoesNotExist).Format,
                userName);

            throw new UserDoesNotExistException(message);
        }

        Log.Debug("User fetched", data: new { id = user.Id });

        return user;
    }

    private static async Task<UserDocument?> GetUser(
        this IDocumentDao<UserDocument> instance,
        Expression<Func<UserDocument, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        var user = await instance.Fetch(filter, cancellationToken).ConfigureAwait(false);

        if (user?.UserStatus is UserStatus.Suspended)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                CompositeFormat.Parse(ServiceExceptions.UserSuspended).Format,
                user.Id);

            throw new UserSuspendedException(message);
        }

        return user;
    }
}
