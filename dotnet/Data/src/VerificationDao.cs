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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using CommonExceptions = Hirameku.Common.Properties.Exceptions;

public class VerificationDao : IVerificationDao
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public VerificationDao(
        IDateTimeProvider dateTimeProvider,
        IOptions<VerificationOptions> options,
        IMongoCollection<UserDocument> userCollection,
        IMongoCollection<Verification> verificationCollection)
    {
        this.DateTimeProvider = dateTimeProvider;
        this.Options = options;
        this.UserCollection = userCollection;
        this.VerificationCollection = verificationCollection;
    }

    private IDateTimeProvider DateTimeProvider { get; }

    private IOptions<VerificationOptions> Options { get; }

    private IMongoCollection<UserDocument> UserCollection { get; }

    private IMongoCollection<Verification> VerificationCollection { get; }

    public async Task<VerificationToken> GenerateVerificationToken(
        string userId,
        string emailAddress,
        VerificationType type,
        CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new { parameters = new { userId, emailAddress, type, cancellationToken } });

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(CommonExceptions.StringNullOrWhiteSpace, nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            throw new ArgumentException(CommonExceptions.StringNullOrWhiteSpace, nameof(emailAddress));
        }

        if (!Enum.IsDefined(type))
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                CompositeFormat.Parse(CommonExceptions.InvalidEnumValue).Format,
                type.ToString(),
                typeof(VerificationType).FullName);

            throw new ArgumentException(message, nameof(type));
        }

        await this.ExpirePriorVerification(userId, emailAddress, type, cancellationToken)
            .ConfigureAwait(false);

        var verification = await this.CreateVerification(emailAddress, userId, type, cancellationToken)
            .ConfigureAwait(false);
        var options = this.Options.Value;
        var pepper = RandomNumberGenerator.GetBytes(options.PepperLength);
        var token = await VerificationToken.Create(verification, pepper, options.HashName, cancellationToken)
            .ConfigureAwait(false);

        Log.Trace("Exiting method", data: new { returnValue = token });

        return token;
    }

    public async Task<VerificationTokenVerificationResult> VerifyToken(
        string userId,
        string emailAddress,
        VerificationType type,
        string token,
        string pepper,
        CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new { parameters = new { userId, emailAddress, type, pepper = "REDACTED", cancellationToken } });

        var verification = await this.GetVerificationToken(
            userId,
            emailAddress,
            type,
            cancellationToken)
            .ConfigureAwait(false);

        VerificationTokenVerificationResult result;

        if (verification != null)
        {
            Log.Info("Verifying token", data: new { userId, emailAddress, type, token });

            result = await this.VerifyToken(token, pepper, verification, cancellationToken)
                .ConfigureAwait(false);

            if (result is VerificationTokenVerificationResult.Verified)
            {
                await this.UpdateUserStatus(userId, type, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            result = VerificationTokenVerificationResult.NotVerified;
        }

        Log.Trace("Exiting method", data: new { retrunValue = result });

        return result;
    }

    private async Task<Verification> CreateVerification(
        string emailAddress,
        string userId,
        VerificationType type,
        CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new { parameters = new { emailAddress, userId, type, cancellationToken } });

        var creationDate = this.DateTimeProvider.UtcNow;
        var options = this.Options.Value;
        var length = options.SaltLength;
        var salt = RandomNumberGenerator.GetBytes(length);
        var verification = new Verification()
        {
            CreationDate = creationDate,
            EmailAddress = emailAddress,
            Salt = salt,
            Type = type,
            UserId = userId,
        };

        var maxVerificationAge = options.MaxVerificationAge;

        if (maxVerificationAge.HasValue)
        {
            verification.ExpirationDate = creationDate + maxVerificationAge.Value;
        }

        await this.VerificationCollection
            .InsertOneAsync(verification, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        Log.Info("Verification created", data: new { verification });
        Log.Trace("Exiting method", data: new { returnValue = verification });

        return verification;
    }

    private async Task ExpirePriorVerification(
        string userId,
        string emailAddress,
        VerificationType type,
        CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new { parameters = new { userId, emailAddress, type, cancellationToken } });

        var verification = await this.GetVerificationToken(userId, emailAddress, type, cancellationToken)
            .ConfigureAwait(false);

        if (verification != null)
        {
            await this.ExpirePriorVerification(verification, false, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            Log.Info("There is no prior verification to expire");
        }

        Log.Trace("Exiting method", data: default(object));
    }

    private async Task ExpirePriorVerification(
        Verification verification,
        bool overrideMinAge,
        CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { verification } });

        var minimumAge = verification.CreationDate + this.Options.Value.MinVerificationAge;
        var now = this.DateTimeProvider.UtcNow;
        var id = verification.Id;
        var verificationMessage = "Verification " + id;

        if (!overrideMinAge && now < minimumAge)
        {
            throw new VerificationException(Exceptions.VerificationTooRecent);
        }
        else if (now < verification.ExpirationDate)
        {
            var update = Builders<Verification>.Update.Set(v => v.ExpirationDate, now);
            var result = await this.VerificationCollection
                .UpdateOneAsync(v => v.Id == id, update, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result.IsModifiedCountAvailable && result.ModifiedCount > 0)
            {
                Log.Info("Verification was successfully expired", data: new { id });
            }
            else
            {
                Log.Warn("Verification was not modified. Did another thread delete it?", data: new { id });
            }
        }
        else
        {
            Log.Info("Verification has already expired", data: new { id });
        }

        Log.Trace("Exiting method", data: default(object));
    }

    private async Task<VerificationTokenVerificationResult> VerifyToken(
        string token,
        string pepper,
        Verification verification,
        CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new { parameters = new { token, pepper = "REDACTED", verification, cancellationToken } });

        VerificationTokenVerificationResult result;

        if (verification != null)
        {
            var generatedToken = await VerificationToken.Create(
                verification,
                Convert.FromBase64String(pepper),
                this.Options.Value.HashName,
                cancellationToken)
                .ConfigureAwait(false);

            result = verification.ExpirationDate <= this.DateTimeProvider.UtcNow
                ? VerificationTokenVerificationResult.TokenExpired
                : token == generatedToken.Token
                    ? VerificationTokenVerificationResult.Verified
                    : VerificationTokenVerificationResult.NotVerified;

            if (result == VerificationTokenVerificationResult.Verified)
            {
                Log.Info("Token was verified", data: new { token });

                await this.ExpirePriorVerification(verification, true, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Log.Info("Token was not verified", data: new { result, token });
            }
        }
        else
        {
            Log.Info("No corresponding Verification was found");

            result = VerificationTokenVerificationResult.NotVerified;
        }

        Log.Trace("Exiting method", data: new { returnValue = result });

        return result;
    }

    private async Task<UserStatus> GetUserStatus(string userId, CancellationToken cancellationToken = default)
    {
        Log.Trace("Entering method", data: new { parameters = new { userId, cancellationToken } });

        var filter = Builders<UserDocument>.Filter.Where(u => u.Id == userId);
        var options = new FindOptions<UserDocument, UserStatus>()
        {
            Projection = Builders<UserDocument>.Projection.Expression(u => u.UserStatus),
        };
        var cursor = await this.UserCollection.FindAsync(filter, options, cancellationToken).ConfigureAwait(false);
        var status = await cursor.FirstAsync(cancellationToken).ConfigureAwait(false);

        Log.Trace("Exiting method", data: new { returnValue = status });

        return status;
    }

    private async Task<Verification?> GetVerificationToken(
        string userId,
        string emailAddress,
        VerificationType type,
        CancellationToken cancellationToken = default)
    {
        Log.Trace(
            "Entering method",
            data: new { parameters = new { userId, emailAddress, type, cancellationToken } });

        using var cursor = await this.VerificationCollection
            .FindAsync(
                v => v.EmailAddress == emailAddress
                && v.ExpirationDate > this.DateTimeProvider.UtcNow
                && v.Type == type
                && v.UserId == userId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var verification = await cursor.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        Log.Trace("Exiting method", data: new { returnValue = verification });

        return verification;
    }

    private async Task UpdateUserStatus(string userId, VerificationType type, CancellationToken cancellationToken)
    {
        var currentStatus = await this.GetUserStatus(userId, cancellationToken).ConfigureAwait(false);
        var newStatus = currentStatus;

        if (type is VerificationType.EmailVerification)
        {
            if (currentStatus is UserStatus.EmailNotVerified)
            {
                newStatus = UserStatus.OK;
            }
            else if (currentStatus is UserStatus.EmailNotVerifiedAndPasswordChangeRequired)
            {
                newStatus = UserStatus.PasswordChangeRequired;
            }
        }
        else if (type is VerificationType.PasswordReset)
        {
            if (currentStatus is UserStatus.PasswordChangeRequired)
            {
                newStatus = UserStatus.OK;
            }
            else if (currentStatus is UserStatus.EmailNotVerifiedAndPasswordChangeRequired)
            {
                newStatus = UserStatus.EmailNotVerified;
            }
        }

        if (newStatus != currentStatus)
        {
            await this.UpdateUserStatus(userId, newStatus, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task UpdateUserStatus(string userId, UserStatus userStatus, CancellationToken cancellationToken)
    {
        Log.Trace("Entering method", data: new { parameters = new { userId, userStatus, cancellationToken, } });

        var result = await this.UserCollection.UpdateOneAsync(
            u => u.Id == userId,
            Builders<UserDocument>.Update.Set(u => u.UserStatus, userStatus),
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.IsModifiedCountAvailable && result.ModifiedCount > 0)
        {
            Log.Info("UserStatus was successfully updated", data: new { userId, userStatus });
        }
        else
        {
            Log.Warn(
                "UserStatus was not updated. Did another thread delete the user?",
                data: new { userId, userStatus });
        }

        Log.Trace("Exiting method", data: default(object));
    }
}
