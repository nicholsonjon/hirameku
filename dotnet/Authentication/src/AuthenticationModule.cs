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

using Autofac;
using AutoMapper;
using FluentValidation;
using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;

public class AuthenticationModule : Module
{
    public AuthenticationModule()
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

        _ = builder.RegisterType<AuthenticationProfile>().As<Profile>();
        _ = builder.RegisterType<ExceptionsProfile>().As<Profile>();
        _ = builder.Register(
            ctx =>
            {
                var profiles = ctx.Resolve<IEnumerable<Profile>>();
                var configuration = new MapperConfiguration(
                    cfg =>
                    {
                        foreach (var profile in profiles)
                        {
                            cfg.AddProfile(profile);
                        }
                    });

                return configuration.CreateMapper();
            })
            .SingleInstance();

        _ = builder.RegisterType<RenewTokenHandler>().As<IRenewTokenHandler>();
        _ = builder.RegisterType<ResetPasswordHandler>().As<IResetPasswordHandler>();
        _ = builder.RegisterType<PersistentTokenIssuer>().As<IPersistentTokenIssuer>();
        _ = builder.RegisterType<RenewTokenModelValidator>().As<IValidator<RenewTokenModel>>();
        _ = builder.RegisterType<ResetPasswordModelValidator>().As<IValidator<ResetPasswordModel>>();
        _ = builder.RegisterType<SecurityTokenIssuer>().As<ISecurityTokenIssuer>();
        _ = builder.RegisterType<SendPasswordResetHandler>().As<ISendPasswordResetHandler>();
        _ = builder.RegisterType<SendPasswordResetModelValidator>().As<IValidator<SendPasswordResetModel>>();
        _ = builder.RegisterType<SignInHandler>().As<ISignInHandler>();
        _ = builder.RegisterType<SignInModelValidator>().As<IValidator<SignInModel>>();
    }
}
