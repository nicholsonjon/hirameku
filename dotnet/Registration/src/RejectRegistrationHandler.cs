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

using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Data;
using Hirameku.Email;
using NLog;
using EmailExceptions = Hirameku.Email.Properties.Exceptions;

public class RejectRegistrationHandler : IRejectRegistrationHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public RejectRegistrationHandler(
        ICachedValueDao cachedValueDao,
        IEmailTokenSerializer emailTokenSerializer,
        IDocumentDao<UserDocument> userDao,
        IVerificationDao verificationDao)
    {
        this.CachedValueDao = cachedValueDao;
        this.EmailTokenSerializer = emailTokenSerializer;
        this.UserDao = userDao;
        this.VerificationDao = verificationDao;
    }

    private ICachedValueDao CachedValueDao { get; }

    private IEmailTokenSerializer EmailTokenSerializer { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    private IVerificationDao VerificationDao { get; }

    public async Task RejectRegistration(string serializedToken, CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { serializedToken = "REDACTED", cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var (pepper, token, userName) = this.EmailTokenSerializer.Deserialize(serializedToken);
        var user = await this.UserDao.GetUserByUserName(userName, cancellationToken).ConfigureAwait(false);
        var userId = user.Id;
        var result = await this.VerificationDao.VerifyToken(
            userId,
            user.EmailAddress,
            VerificationType.EmailVerification,
            token,
            pepper,
            cancellationToken)
            .ConfigureAwait(false);

        if (result is VerificationTokenVerificationResult.Verified)
        {
            Log.ForInfoEvent()
                .Property(LogProperties.Data, new { userId })
                .Message("Registration rejected. User will be suspended.")
                .Log();

            await this.CachedValueDao.SetUserStatus(userId, UserStatus.Suspended).ConfigureAwait(false);
        }
        else
        {
            throw new InvalidTokenException(EmailExceptions.InvalidToken);
        }

        Log.ForTraceEvent()
            .Message(LogMessages.ExitingMethod)
            .Log();
    }
}
