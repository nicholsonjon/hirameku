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

namespace Hirameku.Data;

using Hirameku.Common;
using Hirameku.Data.Properties;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NLog;
using System.Globalization;
using CommonExceptions = Hirameku.Common.Properties.Exceptions;

public class PersistentTokenDao : IPersistentTokenDao
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public PersistentTokenDao(
        IMongoCollection<UserDocument> collection,
        IDateTimeProvider dateTimeProvider,
        IOptions<PersistentTokenOptions> options,
        IPasswordHasher passwordHasher)
    {
        this.Collection = collection;
        this.DateTimeProvider = dateTimeProvider;
        this.Options = options;
        this.PasswordHasher = passwordHasher;
    }

    private IMongoCollection<UserDocument> Collection { get; }

    private IDateTimeProvider DateTimeProvider { get; set; }

    private IOptions<PersistentTokenOptions> Options { get; }

    private IPasswordHasher PasswordHasher { get; }

    public async Task<DateTime> SavePersistentToken(
        string userId,
        string clientId,
        string clientToken,
        CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new
                {
                    userId,
                    clientId,
                    clientToken = "REDACTED",
                    cancellationToken,
                },
            });

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(CommonExceptions.StringNullOrWhiteSpace, nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException(CommonExceptions.StringNullOrWhiteSpace, nameof(clientId));
        }

        if (string.IsNullOrWhiteSpace(clientToken))
        {
            throw new ArgumentException(CommonExceptions.StringNullOrWhiteSpace, nameof(clientToken));
        }

        var user = await this.GetUser(userId, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                CommonExceptions.UserIdDoesNotExist,
                userId);

            throw new UserDoesNotExistException(message);
        }

        var passwordHash = user.PasswordHash;

        if (passwordHash == null)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                Exceptions.NoStoredPasswordHash,
                userId);

            throw new InvalidOperationException(message);
        }

        var persistentTokens = user.PersistentTokens;
        var persistentToken = persistentTokens?.SingleOrDefault(
            pt => pt.ClientId == clientId && pt.ExpirationDate > this.DateTimeProvider.UtcNow)
            ?? new PersistentToken() { ClientId = clientId };
        var options = this.Options.Value;
        var hashPasswordResult = this.PasswordHasher.HashPassword(clientId + clientToken, passwordHash.Hash);
        var expirationDate = this.DateTimeProvider.UtcNow + options.MaxTokenAge;

        persistentToken.ExpirationDate = expirationDate;
        persistentToken.Hash = hashPasswordResult.Hash;

        await this.UpsertToken(userId, persistentToken, cancellationToken).ConfigureAwait(false);
        await this.PurgeExpiredTokens(userId, cancellationToken).ConfigureAwait(false);

        Log.Trace("Exiting method", data: new { returnValue = expirationDate });

        return expirationDate;
    }

    public async Task<PersistentTokenVerificationResult> VerifyPersistentToken(
        string userId,
        string clientId,
        string clientToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(CommonExceptions.StringNullOrWhiteSpace, nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException(CommonExceptions.StringNullOrWhiteSpace, nameof(clientId));
        }

        if (string.IsNullOrWhiteSpace(clientToken))
        {
            throw new ArgumentException(CommonExceptions.StringNullOrWhiteSpace, nameof(clientToken));
        }

        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new
                {
                    userId,
                    clientId,
                    clientToken = "REDACTED",
                    cancellationToken,
                },
            });

        var user = await this.GetUser(userId, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                CommonExceptions.UserIdDoesNotExist,
                userId);

            throw new UserDoesNotExistException(message);
        }

        var passwordHash = user.PasswordHash;
        var persistentTokens = user.PersistentTokens;
        var persistentToken = persistentTokens?.SingleOrDefault(
            pt => pt.ClientId == clientId && pt.ExpirationDate > this.DateTimeProvider.UtcNow);
        PersistentTokenVerificationResult result;

        if (passwordHash != null && persistentToken != null)
        {
            var verifyPasswordResult = this.PasswordHasher.VerifyPassword(
                PasswordHashVersion.Current,
                passwordHash.Hash,
                persistentToken.Hash,
                clientId + clientToken);

            // VerifiedAndRehashRequired can't happen in this context because persistent tokens are always hashed
            // and verified using PasswordHashVersion.Current. If we upgrade the current version, that'll break all
            // PersistentTokens that were hashed using the old PasswordHashVersion, which is exactly what we want to
            // happen. We only support versioning for user passwords.
            result = verifyPasswordResult == VerifyPasswordResult.Verified
                ? PersistentTokenVerificationResult.Verified
                : PersistentTokenVerificationResult.NotVerified;
        }
        else
        {
            result = PersistentTokenVerificationResult.NoTokenAvailable;
        }

        await this.PurgeExpiredTokens(user.Id, cancellationToken).ConfigureAwait(false);

        Log.Trace("Exiting method", data: default(object));

        return result;
    }

    private async Task<UserDocument> GetUser(string userId, CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { userId, cancellationToken } });

        var cursor = await this.Collection.FindAsync(u => u.Id == userId, default, cancellationToken)
            .ConfigureAwait(false);
        var user = await cursor.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        Log.Trace("Exiting method", data: new { returnValue = user });

        return user;
    }

    private async Task PurgeExpiredTokens(string userId, CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { userId, cancellationToken } });

        _ = await this.Collection.DeleteManyAsync(
            u => u.Id == userId
                && u.PersistentTokens != null
                && u.PersistentTokens.Any(pt => pt.ExpirationDate <= this.DateTimeProvider.UtcNow),
            cancellationToken)
            .ConfigureAwait(false);

        Log.Trace("Exiting method", data: default(object));
    }

    private async Task UpsertToken(
        string userId,
        PersistentToken persistentToken,
        CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { userId, persistentToken } });

        // equivalent Mongo query: {
        //     _id: { $eq: <userid> },
        //     PersistentTokens: { $elemMatch: { ClientId: { $eq: <clientId> } }
        // }
        var userFilter = Builders<UserDocument>.Filter;
        var userIdFilter = userFilter.Eq(u => u.Id, userId);
        var clientId = persistentToken.ClientId;
        var clientIdFilter = userFilter.ElemMatch(u => u.PersistentTokens, pt => pt.ClientId == clientId);
        var update = Builders<UserDocument>.Update.Set(
            u => u.PersistentTokens!.FirstMatchingElement(), persistentToken);
        var result = await this.Collection.UpdateOneAsync(
            userIdFilter & clientIdFilter,
            update,
            new UpdateOptions() { IsUpsert = true },
            cancellationToken)
            .ConfigureAwait(false);

        if (result.IsModifiedCountAvailable && result.ModifiedCount > 0)
        {
            Log.Info("PersistentToken was successfully upserted", data: new { userId, clientId });
        }
        else
        {
            Log.Warn(
                "PersistentToken was not modified. Did another thread delete the user?",
                data: new { userId, clientId });
        }

        Log.Trace("Exiting method", data: default(object));
    }
}
