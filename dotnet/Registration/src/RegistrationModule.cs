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

using Autofac;
using AutoMapper;
using FluentValidation;
using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;

public class RegistrationModule : Module
{
    public RegistrationModule()
    {
    }

    protected override void Load(ContainerBuilder builder)
    {
        _ = builder.RegisterModule<CachingModule>().IfNotRegistered(typeof(CachingModule));
        _ = builder.RegisterModule<CommonModule>().IfNotRegistered(typeof(CommonModule));
        _ = builder.RegisterModule<CommonServiceModule>().IfNotRegistered(typeof(CommonServiceModule));
        _ = builder.RegisterModule<DataModule>().IfNotRegistered(typeof(DataModule));
        _ = builder.RegisterModule<EmailModule>().IfNotRegistered(typeof(EmailModule));
        _ = builder.RegisterModule<RecaptchaModule>().IfNotRegistered(typeof(RecaptchaModule));

        _ = builder.RegisterType<ExceptionsProfile>().As<Profile>();
        _ = builder.RegisterType<RegistrationProfile>().As<Profile>();
        _ = builder.RegisterMapper().SingleInstance();

        _ = builder.RegisterType<IsUserNameAvailableHandler>().As<IIsUserNameAvailableHandler>();
        _ = builder.RegisterType<RegisterHandler>().As<IRegisterHandler>();
        _ = builder.RegisterType<RegisterModelValidator>().As<IValidator<RegisterModel>>();
        _ = builder.RegisterType<RejectRegistrationHandler>().As<IRejectRegistrationHandler>();
        _ = builder.RegisterType<ResendVerificationEmailHandler>().As<IResendVerificationHandler>();
        _ = builder.RegisterType<ResendVerificationEmailModelValidator>().As<IValidator<ResendVerificationEmailModel>>();
        _ = builder.RegisterType<ValidatePasswordHandler>().As<IValidatePasswordHandler>();
        _ = builder.RegisterType<VerifyEmailAddressHandler>().As<IVerifyEmailAddressHandler>();
    }
}
