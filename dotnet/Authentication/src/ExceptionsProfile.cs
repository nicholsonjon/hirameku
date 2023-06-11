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

namespace Hirameku.Authentication;

using AutoMapper;
using FluentValidation;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Common.Service.Properties;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Microsoft.AspNetCore.Mvc;
using System.Net;

public class ExceptionsProfile : Profile
{
    public ExceptionsProfile()
    {
        _ = this.CreateMap<ArgumentException, ProblemDetails>()
            .ForMember(d => d.Detail, o => o.MapFrom(s => s.Message))
            .ForMember(d => d.Extensions, o => o.Ignore())
            .ForMember(d => d.Instance, o => o.MapFrom(_ => ErrorCodes.RequestValidationFailed))
            .ForMember(d => d.Status, o => o.MapFrom(_ => HttpStatusCode.BadRequest))
            .ForMember(d => d.Title, o => o.MapFrom(_ => Resources.RequestValidationFailed));
        _ = this.CreateMap<EmailAddressNotVerifiedException, ProblemDetails>()
            .ForMember(d => d.Detail, o => o.MapFrom(s => s.Message))
            .ForMember(d => d.Extensions, o => o.Ignore())
            .ForMember(d => d.Instance, o => o.MapFrom(_ => ErrorCodes.EmailAddressNotVerified))
            .ForMember(d => d.Status, o => o.MapFrom(_ => HttpStatusCode.Forbidden))
            .ForMember(d => d.Title, o => o.MapFrom(_ => Resources.EmailAddressNotVerified));
        _ = this.CreateMap<InvalidTokenException, ProblemDetails>()
            .ForMember(d => d.Detail, o => o.MapFrom(s => s.Message))
            .ForMember(d => d.Extensions, o => o.Ignore())
            .ForMember(d => d.Instance, o => o.MapFrom(_ => ErrorCodes.InvalidToken))
            .ForMember(d => d.Status, o => o.MapFrom(_ => HttpStatusCode.BadRequest))
            .ForMember(d => d.Title, o => o.MapFrom(_ => Resources.InvalidToken));
        _ = this.CreateMap<PasswordException, ProblemDetails>()
            .ForMember(d => d.Detail, o => o.MapFrom(s => s.Message))
            .ForMember(d => d.Extensions, o => o.Ignore())
            .ForMember(d => d.Instance, o => o.MapFrom(_ => ErrorCodes.PasswordChangeRejected))
            .ForMember(d => d.Status, o => o.MapFrom(_ => HttpStatusCode.BadRequest))
            .ForMember(d => d.Title, o => o.MapFrom(_ => Resources.PasswordChangeRejected));
        _ = this.CreateMap<RecaptchaVerificationFailedException, ProblemDetails>()
            .ForMember(d => d.Detail, o => o.MapFrom(s => s.Message))
            .ForMember(d => d.Extensions, o => o.Ignore())
            .ForMember(d => d.Instance, o => o.MapFrom(_ => ErrorCodes.RecaptchaVerificationFailed))
            .ForMember(d => d.Status, o => o.MapFrom(_ => HttpStatusCode.BadRequest))
            .ForMember(d => d.Title, o => o.MapFrom(_ => Resources.RecaptchaVerificationFailed));
        _ = this.CreateMap<UserDoesNotExistException, ProblemDetails>()
            .ForMember(d => d.Detail, o => o.MapFrom(s => s.Message))
            .ForMember(d => d.Extensions, o => o.Ignore())
            .ForMember(d => d.Instance, o => o.MapFrom(_ => ErrorCodes.UserDoesNotExist))
            .ForMember(d => d.Status, o => o.MapFrom(_ => HttpStatusCode.BadRequest))
            .ForMember(d => d.Title, o => o.MapFrom(_ => Resources.UserDoesNotExist));
        _ = this.CreateMap<UserSuspendedException, ProblemDetails>()
            .ForMember(d => d.Detail, o => o.MapFrom(s => s.Message))
            .ForMember(d => d.Extensions, o => o.Ignore())
            .ForMember(d => d.Instance, o => o.MapFrom(_ => ErrorCodes.UserSuspended))
            .ForMember(d => d.Status, o => o.MapFrom(_ => HttpStatusCode.Forbidden))
            .ForMember(d => d.Title, o => o.MapFrom(_ => Resources.UserSuspended));
        _ = this.CreateMap<ValidationException, ProblemDetails>()
            .ForMember(d => d.Detail, o => o.MapFrom(s => s.Message))
            .ForMember(d => d.Extensions, o => o.Ignore())
            .ForMember(d => d.Instance, o => o.MapFrom(_ => ErrorCodes.RequestValidationFailed))
            .ForMember(d => d.Status, o => o.MapFrom(_ => HttpStatusCode.BadRequest))
            .ForMember(d => d.Title, o => o.MapFrom(_ => Resources.RequestValidationFailed));
        _ = this.CreateMap<VerificationException, ProblemDetails>()
            .ForMember(d => d.Detail, o => o.MapFrom(s => s.Message))
            .ForMember(d => d.Extensions, o => o.Ignore())
            .ForMember(d => d.Instance, o => o.MapFrom(_ => ErrorCodes.VerificationFailed))
            .ForMember(d => d.Status, o => o.MapFrom(_ => HttpStatusCode.BadRequest))
            .ForMember(d => d.Title, o => o.MapFrom(_ => Resources.VerificationFailed));
    }
}
