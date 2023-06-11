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

namespace Hirameku.Common.Service;

using Hirameku.Data;
using Microsoft.Extensions.Options;
using NLog;
using System.Security.Cryptography;

public class PersistentTokenIssuer : IPersistentTokenIssuer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public PersistentTokenIssuer(
        IPersistentTokenDao dao,
        IOptions<PersistentTokenOptions> options,
        IUniqueIdGenerator uniqueIdGenerator)
    {
        this.Dao = dao;
        this.Options = options;
        this.UniqueIdGenerator = uniqueIdGenerator;
    }

    private IPersistentTokenDao Dao { get; }

    private IOptions<PersistentTokenOptions> Options { get; }

    private IUniqueIdGenerator UniqueIdGenerator { get; }

    public async Task<PersistentTokenModel> Issue(string userId, CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { userId, cancellationToken } });

        var clientId = this.UniqueIdGenerator.GenerateUniqueId();
        var tokenBytes = RandomNumberGenerator.GetBytes(this.Options.Value.ClientTokenLength);
        var clientToken = Convert.ToBase64String(tokenBytes);
        var expirationDate = await this.Dao.SavePersistentToken(
            userId,
            clientId,
            clientToken,
            cancellationToken)
            .ConfigureAwait(false);

        Log.Debug("Persistent token issued", data: new { userId, clientId, expirationDate });
        Log.Trace(
            "Exiting method",
            data: new
            {
                returnValue = new
                {
                    clientId,
                    clientToken = "REDACTED",
                    expirationDate,
                    userId,
                },
            });

        return new PersistentTokenModel()
        {
            ClientId = clientId,
            ClientToken = clientToken,
            ExpirationDate = expirationDate,
            UserId = userId,
        };
    }
}
