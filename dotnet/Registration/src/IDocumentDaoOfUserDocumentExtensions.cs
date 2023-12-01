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

namespace Hirameku.Registration;

using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Registration.Properties;
using NLog;
using NLog.Fluent;
using System.Globalization;
using System.Text;
using CommonExceptions = Hirameku.Common.Service.Properties.Exceptions;

public static class IDocumentDaoOfUserDocumentExtensions
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static Task<UserDocument> GetUserByEmail(
        this IDocumentDao<UserDocument> instance,
        string emailAddress,
        CancellationToken cancellationToken = default)
    {
        Log.Debug("Fetching User document", data: new { instance, emailAddress, cancellationToken });

        ArgumentNullException.ThrowIfNull(instance);

        return GetUser(instance, u => u.EmailAddress, emailAddress, cancellationToken);
    }

    public static Task<UserDocument> GetUserByUserName(
        this IDocumentDao<UserDocument> instance,
        string userName,
        CancellationToken cancellationToken = default)
    {
        Log.Debug("Fetching User document", data: new { instance, userName, cancellationToken });

        ArgumentNullException.ThrowIfNull(instance);

        return GetUser(instance, u => u.UserName, userName, cancellationToken);
    }

    private static async Task<UserDocument> GetUser(
        IDocumentDao<UserDocument> userDao,
        Func<UserDocument, string> member,
        string value,
        CancellationToken cancellationToken)
    {
        var user = await userDao.Fetch(u => member(u) == value, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                CompositeFormat.Parse(Exceptions.UserDoesNotExist).Format,
                value);

            throw new UserDoesNotExistException(message);
        }
        else if (user.UserStatus is UserStatus.Suspended)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                CompositeFormat.Parse(CommonExceptions.UserSuspended).Format,
                user.UserName);

            throw new UserSuspendedException(message);
        }

        Log.Debug("User fetched", data: new { user.Id });

        return user;
    }
}
