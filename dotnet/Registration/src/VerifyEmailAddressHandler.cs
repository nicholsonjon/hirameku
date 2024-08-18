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

using AutoMapper;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Data;
using Hirameku.Email;
using NLog;

public class VerifyEmailAddressHandler : IVerifyEmailAddressHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public VerifyEmailAddressHandler(
        IEmailTokenSerializer emailTokenSerializer,
        IMapper mapper,
        IDocumentDao<UserDocument> userDao,
        IVerificationDao verificationDao)
    {
        this.EmailTokenSerializer = emailTokenSerializer;
        this.Mapper = mapper;
        this.UserDao = userDao;
        this.VerificationDao = verificationDao;
    }

    private IEmailTokenSerializer EmailTokenSerializer { get; }

    private IMapper Mapper { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    private IVerificationDao VerificationDao { get; }

    public async Task<EmailVerificationResult> VerifyEmaiAddress(
        string serializedToken,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { serializedToken = "REDACTED", cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var (pepper, token, userName) = this.EmailTokenSerializer.Deserialize(serializedToken);
        var user = await this.UserDao.GetUserByUserName(userName, cancellationToken).ConfigureAwait(false);
        var tokenResult = await this.VerificationDao.VerifyToken(
            user.Id,
            user.EmailAddress,
            VerificationType.EmailVerification,
            token,
            pepper,
            cancellationToken)
            .ConfigureAwait(false);
        var emailResult = this.Mapper.Map<EmailVerificationResult>(tokenResult);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, emailResult)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return emailResult;
    }
}
