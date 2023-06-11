﻿// Hirameku is a cloud-native, vendor-agnostic, serverless application for
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

namespace Hirameku.Authentication;

using FluentValidation;
using Hirameku.Common;
using Hirameku.Common.Service;

public class ResetPasswordModelValidator : AbstractValidator<ResetPasswordModel>
{
    public ResetPasswordModelValidator(IPasswordValidator passwordValidator)
    {
        _ = this.RuleFor(m => m.Password)
            .CustomAsync(passwordValidator.ValidateAsync);
        _ = this.RuleFor(m => m.RecaptchaResponse)
            .NotEmpty();
        _ = this.RuleFor(m => m.SerializedToken)
            .NotEmpty()
            .Matches(Regexes.Base64String);
    }
}
