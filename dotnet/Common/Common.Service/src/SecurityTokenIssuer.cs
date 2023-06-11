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

using Hirameku.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Fluent;
using System.IdentityModel.Tokens.Jwt;

public class SecurityTokenIssuer : ISecurityTokenIssuer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public SecurityTokenIssuer(IDateTimeProvider dateTimeProvider, IOptions<SecurityTokenOptions> options)
    {
        this.DateTimeProvider = dateTimeProvider;
        this.Options = options;
    }

    private IDateTimeProvider DateTimeProvider { get; }

    private IOptions<SecurityTokenOptions> Options { get; }

    public SecurityToken Issue(string userId, User user, DateTime? validTo = default)
    {
        Log.Trace(
            "Entering method",
            data: new { parameters = new { userId, user, validTo } });

        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var options = this.Options.Value;
        var now = this.DateTimeProvider.UtcNow;
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Convert.FromBase64String(options.SecretKey)),
            options.SecurityAlgorithm);

        var descriptor = new SecurityTokenDescriptor
        {
            Audience = options.Audience?.ToString(),
            Claims = new Dictionary<string, object>()
            {
                { JwtRegisteredClaimNames.Name, user.Name },
                { JwtRegisteredClaimNames.Sub, user.UserName },
                { PrivateClaims.UserId, userId },
            },
            Expires = validTo ?? now + options.TokenExpiry,
            IssuedAt = now,
            Issuer = options.Issuer?.ToString(),
            NotBefore = now,
            SigningCredentials = signingCredentials,
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);

        Log.Info("JWT issued", data: new { token });
        Log.Trace("Exiting method", data: new { returnValue = token });

        return token;
    }
}
