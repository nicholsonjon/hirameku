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

namespace Hirameku.User;

using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Common.Service;
using Hirameku.Data;
using NLog;
using System.Globalization;
using System.Text;
using UserExceptions = Hirameku.User.Properties.Exceptions;

public class GetUserHandler : IGetUserHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public GetUserHandler(IDocumentDao<UserDocument> userDao)
    {
        this.UserDao = userDao;
    }

    private IDocumentDao<UserDocument> UserDao { get; }

    public async Task<User?> GetUser(
        Authenticated<Unit> authenticatedModel,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { authenticatedModel, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        ArgumentNullException.ThrowIfNull(authenticatedModel);

        var userId = authenticatedModel.User.GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                CompositeFormat.Parse(UserExceptions.MissingClaim).Format,
                userId);

            throw new ArgumentException(message, nameof(authenticatedModel));
        }

        var user = await this.UserDao.Fetch(userId, cancellationToken).ConfigureAwait(false);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, user)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return user;
    }
}
