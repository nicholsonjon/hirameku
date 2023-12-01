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
using NLog;
using System.Globalization;
using System.Text;
using System.Threading;
using CommonExceptions = Hirameku.Common.Properties.Exceptions;

public class PasswordDao : IPasswordDao
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public PasswordDao(
        IMongoCollection<UserDocument> collection,
        IDateTimeProvider dateTimeProvider,
        IOptions<PasswordOptions> options,
        IPasswordHasher passwordHasher)
    {
        this.Collection = collection;
        this.DateTimeProvider = dateTimeProvider;
        this.Options = options;
        this.PasswordHasher = passwordHasher;
    }

    private IMongoCollection<UserDocument> Collection { get; }

    private IDateTimeProvider DateTimeProvider { get; }

    private IOptions<PasswordOptions> Options { get; }

    private IPasswordHasher PasswordHasher { get; }

    public async Task SavePassword(string userId, string password, CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new { userId, password = "REDACTED", cancellationToken },
            });

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(CommonExceptions.StringNullOrWhiteSpace, nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(CommonExceptions.StringNullOrWhiteSpace, nameof(password));
        }

        var user = await this.GetUser(userId, cancellationToken).ConfigureAwait(false);

        if (user != null)
        {
            var passwordHash = user.PasswordHash ?? new PasswordHash();
            var options = this.Options.Value;

            if (this.DateTimeProvider.UtcNow > passwordHash.LastChangeDate + options.MinPasswordAge)
            {
                var hash = passwordHash.Hash;

                if (hash.Length != 0 && options.DisallowSavingIdenticalPasswords)
                {
                    var verifyResult = this.PasswordHasher.VerifyPassword(
                        passwordHash.Version,
                        passwordHash.Salt,
                        hash,
                        password);

                    if (verifyResult == VerifyPasswordResult.Verified)
                    {
                        throw new PasswordException(Exceptions.PasswordIsIdentical);
                    }
                }

                await this.DoSavePassword(userId, password, cancellationToken).ConfigureAwait(false);

                if (user.UserStatus is UserStatus.PasswordChangeRequired)
                {
                    await this.UpdateUserStatus(userId, UserStatus.OK, cancellationToken).ConfigureAwait(false);
                }
                else if (user.UserStatus is UserStatus.EmailNotVerifiedAndPasswordChangeRequired)
                {
                    await this.UpdateUserStatus(userId, UserStatus.EmailNotVerified, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                throw new PasswordException(Exceptions.PasswordChangeTooRecent);
            }
        }
        else
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                CompositeFormat.Parse(CommonExceptions.UserIdDoesNotExist).Format,
                userId);

            throw new UserDoesNotExistException(message);
        }

        Log.Trace("Exiting method", data: default(object));
    }

    public async Task<PasswordVerificationResult> VerifyPassword(
        string userId,
        string password,
        CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new { userId, password = "REDACTED", cancellationToken },
            });

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(CommonExceptions.StringNullOrWhiteSpace, nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(CommonExceptions.StringNullOrWhiteSpace, nameof(password));
        }

        var user = await this.GetUser(userId, cancellationToken).ConfigureAwait(false);
        PasswordVerificationResult result;

        if (user != null)
        {
            var passwordHash = user.PasswordHash;
            var hash = passwordHash?.Hash;

            if ((hash?.Length ?? 0) != 0)
            {
                var passwordHasher = this.PasswordHasher;
                var verifyResult = passwordHasher.VerifyPassword(
                    passwordHash!.Version,
                    passwordHash.Salt,
                    hash!,
                    password);

                switch (verifyResult)
                {
                    case VerifyPasswordResult.VerifiedAndRehashRequired:

                        await this.DoSavePassword(user.Id, password, cancellationToken).ConfigureAwait(false);
                        goto case VerifyPasswordResult.Verified;

                    case VerifyPasswordResult.Verified:

                        var expirationDate = passwordHash.ExpirationDate;
                        result = !expirationDate.HasValue || expirationDate.Value > this.DateTimeProvider.UtcNow
                            ? PasswordVerificationResult.Verified
                            : PasswordVerificationResult.VerifiedAndExpired;
                        break;

                    default:

                        result = PasswordVerificationResult.NotVerified;
                        break;
                }
            }
            else
            {
                result = PasswordVerificationResult.NotVerified;
            }
        }
        else
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                CompositeFormat.Parse(CommonExceptions.UserIdDoesNotExist).Format,
                userId);

            throw new UserDoesNotExistException(message);
        }

        Log.Trace("Exiting method", data: default(object));

        return result;
    }

    private async Task DoSavePassword(string userId, string password, CancellationToken cancellationToken)
    {
        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new { userId, password = "REDACTED", cancellationToken },
            });

        var lastChangeDate = this.DateTimeProvider.UtcNow;
        var hashResult = this.PasswordHasher.HashPassword(password);
        var passwordHash = new PasswordHash()
        {
            LastChangeDate = lastChangeDate,
            Hash = hashResult.Hash,
            Salt = hashResult.Salt,
            Version = hashResult.Version,
        };

        var maxPasswordAge = this.Options.Value.MaxPasswordAge;

        if (maxPasswordAge.HasValue)
        {
            passwordHash.ExpirationDate = lastChangeDate + maxPasswordAge.Value;
        }

        var update = Builders<UserDocument>.Update.Set(u => u.PasswordHash, passwordHash);
        var updateResult = await this.Collection
            .UpdateOneAsync(u => u.Id == userId, update, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (updateResult.IsModifiedCountAvailable && updateResult.ModifiedCount > 0)
        {
            Log.Info("Successfully updated the user's password", data: new { userId });
        }
        else
        {
            Log.Warn(
                "The user's password was not modified. Did another thread delete the user?",
                data: new { userId });
        }

        Log.Trace("Exiting method", data: default(object));
    }

    private async Task<UserDocument> GetUser(string userId, CancellationToken cancellationToken)
    {
        Log.Trace("Entering method", data: new { parameters = new { userId, cancellationToken } });

        using var cursor = await this.Collection
            .FindAsync(u => u.Id == userId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var user = await cursor.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        Log.Trace("Exiting method", data: new { returnValue = user });

        return user;
    }

    private async Task UpdateUserStatus(string userId, UserStatus userStatus, CancellationToken cancellationToken)
    {
        Log.Trace("Entering method", data: new { parameters = new { userId, userStatus, cancellationToken } });

        var update = Builders<UserDocument>.Update.Set(u => u.UserStatus, userStatus);
        var updateResult = await this.Collection.UpdateOneAsync(
            u => u.Id == userId,
            update,
            default,
            cancellationToken)
            .ConfigureAwait(false);

        if (updateResult.IsModifiedCountAvailable && updateResult.ModifiedCount > 0)
        {
            Log.Info("Successfully updated the UserStatus", data: new { userId, userStatus });
        }
        else
        {
            Log.Warn(
                "The UserStatus was not modified. Did another thread delete the user?",
                data: new { userId, userStatus });
        }

        Log.Trace("Exiting method", data: default(object));
    }
}
