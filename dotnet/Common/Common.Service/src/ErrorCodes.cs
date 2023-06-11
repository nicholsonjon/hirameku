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

public static class ErrorCodes
{
    public const string EmailAddressAlreadyVerified = "urn:hirameku:error:emailAddressAlreadyVerified";
    public const string EmailAddressNotVerified = "urn:hirameku:error:emailAddressNotVerified";
    public const string InvalidToken = "urn:hirameku:error:invalidToken";
    public const string PasswordChangeRejected = "urn:hirameku:error:passwordChangeRejected";
    public const string RecaptchaVerificationFailed = "urn:hirameku:error:recaptchaVerificationFailed";
    public const string RequestValidationFailed = "urn:hirameku:error:requestValidationFailed";
    public const string UnexpectedError = "urn:hirameku:error:unexpectedError";
    public const string UserAlreadyExists = "urn:hirameku:error:userAlreadyExists";
    public const string UserDoesNotExist = "urn:hirameku:error:userDoesNotExist";
    public const string UserSuspended = "urn:hirameku:error:userSuspended";
    public const string VerificationFailed = "urn:hirameku:error:verificationFailed";
}
