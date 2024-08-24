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

namespace Hirameku.User;

using Autofac;
using FluentValidation;
using Hirameku.Caching;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;

public class UserModule : Module
{
    public UserModule()
    {
    }

    protected override void Load(ContainerBuilder builder)
    {
        _ = builder.RegisterModule<CachingModule>().IfNotRegistered(typeof(CachingModule));
        _ = builder.RegisterModule<CommonServiceModule>().IfNotRegistered(typeof(CommonServiceModule));
        _ = builder.RegisterModule<DataModule>().IfNotRegistered(typeof(DataModule));
        _ = builder.RegisterModule<EmailModule>().IfNotRegistered(typeof(EmailModule));

        _ = builder.Register(
            c =>
            {
                var modelValidator = new ChangePasswordModelValidator(c.Resolve<IPasswordValidator>());
                return new AuthenticatedValidator<ChangePasswordModel>(modelValidator);
            })
            .As<IValidator<Authenticated<ChangePasswordModel>>>();
        _ = builder.Register(
            c =>
            {
                var modelValidator = new UpdateEmailAddressModelValidator();
                return new AuthenticatedValidator<UpdateEmailAddressModel>(modelValidator);
            })
            .As<IValidator<Authenticated<UpdateEmailAddressModel>>>();
        _ = builder.Register(
            c =>
            {
                var modelValidator = new UpdateNameModelValidator();
                return new AuthenticatedValidator<UpdateNameModel>(modelValidator);
            })
            .As<IValidator<Authenticated<UpdateNameModel>>>();
        _ = builder.Register(
            c =>
            {
                var modelValidator = new UpdateUserNameModelValidator();
                return new AuthenticatedValidator<UpdateUserNameModel>(modelValidator);
            })
            .As<IValidator<Authenticated<UpdateUserNameModel>>>();

        _ = builder.RegisterType<ChangePasswordHandler>().As<IChangePasswordHandler>();
        _ = builder.RegisterType<DeleteUserHandler>().As<IDeleteUserHandler>();
        _ = builder.RegisterType<GetUserHandler>().As<IGetUserHandler>();
        _ = builder.RegisterType<UpdateEmailAddressHandler>().As<IUpdateEmailAddressHandler>();
        _ = builder.RegisterType<UpdateNameHandler>().As<IUpdateNameHandler>();
        _ = builder.RegisterType<UpdateUserNameHandler>().As<IUpdateUserNameHandler>();
    }
}
