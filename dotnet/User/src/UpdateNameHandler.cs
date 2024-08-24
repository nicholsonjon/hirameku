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

using FluentValidation;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Common.Service;
using Hirameku.Data;
using Microsoft.IdentityModel.Tokens;
using NLog;
using System.Globalization;
using System.Text;

public class UpdateNameHandler : IUpdateNameHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public UpdateNameHandler(
        ISecurityTokenIssuer securityTokenIssuer,
        IValidator<Authenticated<UpdateNameModel>> updateNameModelValidator,
        IDocumentDao<UserDocument> userDao)
    {
        this.SecurityTokenIssuer = securityTokenIssuer;
        this.UpdateNameModelValidator = updateNameModelValidator;
        this.UserDao = userDao;
    }

    private ISecurityTokenIssuer SecurityTokenIssuer { get; }

    private IValidator<Authenticated<UpdateNameModel>> UpdateNameModelValidator { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    public async Task<SecurityToken> UpdateName(
        Authenticated<UpdateNameModel> authenticatedModel,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { authenticatedModel, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        ArgumentNullException.ThrowIfNull(authenticatedModel);

        await this.UpdateNameModelValidator.ValidateAndThrowAsync(authenticatedModel, cancellationToken)
            .ConfigureAwait(false);

        var userId = authenticatedModel.User.GetUserId()!;
        var userDao = this.UserDao;
        var user = await userDao.Fetch(userId, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                CompositeFormat.Parse(Exceptions.UserIdDoesNotExist).Format,
                userId);

            throw new UserDoesNotExistException(message);
        }

        await userDao.Update(
            userId,
            u => u.Name,
            authenticatedModel.Model.Name,
            cancellationToken)
            .ConfigureAwait(false);

        var sessionToken = this.SecurityTokenIssuer.Issue(userId, user);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, sessionToken)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return sessionToken;
    }
}
