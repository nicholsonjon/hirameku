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
using Hirameku.Common.Properties;
using Hirameku.Data;
using NLog;
using System.Text.RegularExpressions;

public partial class IsUserNameAvailableHandler : IIsUserNameAvailableHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public IsUserNameAvailableHandler(IDocumentDao<UserDocument> userDao)
    {
        this.UserDao = userDao;
    }

    private IDocumentDao<UserDocument> UserDao { get; }

    public async Task<bool> IsUserNameAvailable(string userName, CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { userName, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var isUserNameAvailable = GeneratedUserNameRegex().IsMatch(userName);

        if (isUserNameAvailable)
        {
            var count = await this.UserDao.GetCount(u => u.UserName == userName, cancellationToken)
                .ConfigureAwait(false);

            isUserNameAvailable = count == 0;
        }

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, isUserNameAvailable)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return isUserNameAvailable;
    }

    [GeneratedRegex(Regexes.UserName)]
    private static partial Regex GeneratedUserNameRegex();
}
