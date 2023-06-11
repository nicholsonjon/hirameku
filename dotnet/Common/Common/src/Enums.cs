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

namespace Hirameku.Common;
public enum AuthenticationResult
{
    NotAuthenticated,
    Authenticated,
    PasswordExpired,
    LockedOut,
    Suspended,
}

public enum Disposition
{
    Scheduled,
    Remembered,
    Forgot,
    Retried,
    Buried,
    Burned,
}

public enum Interval
{
    Beginner1,
    Beginner2,
    Beginner3,
    Intermediate1,
    Intermediate2,
    Intermediate3,
    Advanced1,
    Advanced2,
    Advanced3,
    Expert1,
    Expert2,
    Expert3,
    Master,
}

public enum UserStatus
{
    EmailNotVerified,
    PasswordChangeRequired,
    EmailNotVerifiedAndPasswordChangeRequired,
    OK,
    Suspended,
}
