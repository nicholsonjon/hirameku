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

using Hirameku.Caching;
using Hirameku.Common.Service.Properties;
using System.Globalization;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

public class UserStatusValidator : IUserStatusValidator
{
    public UserStatusValidator(ICachedValueDao cachedValueDao)
    {
        this.CachedValueDao = cachedValueDao;
    }

    private ICachedValueDao CachedValueDao { get; }

    public async Task ValidateUserStatus(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var userId = user.GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                Exceptions.MissingRequiredClaim,
                PrivateClaims.UserId);

            throw new ArgumentException(message, nameof(user));
        }

        var userStatus = await this.CachedValueDao.GetUserStatus(userId, cancellationToken).ConfigureAwait(false);

        if (userStatus is UserStatus.EmailNotVerified)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                Exceptions.EmailAddressNotVerified,
                userId);

            throw new EmailAddressNotVerifiedException(message);
        }
        else if (userStatus is UserStatus.PasswordChangeRequired
            or UserStatus.EmailNotVerifiedAndPasswordChangeRequired)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                Exceptions.UserMustChangePassword,
                userId);

            throw new UserMustChangePasswordException(message);
        }
        else if (userStatus is UserStatus.Suspended)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                Exceptions.UserSuspended,
                userId);

            throw new UserSuspendedException(message);
        }
    }
}
