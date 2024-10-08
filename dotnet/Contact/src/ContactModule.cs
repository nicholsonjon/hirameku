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

namespace Hirameku.Contact;

using Autofac;
using FluentValidation;
using Hirameku.Common.Service;
using Hirameku.Email;
using Hirameku.Recaptcha;

public class ContactModule : Module
{
    public ContactModule()
    {
    }

    protected override void Load(ContainerBuilder builder)
    {
        _ = builder.RegisterModule<EmailModule>().IfNotRegistered(typeof(EmailModule));
        _ = builder.RegisterModule<RecaptchaModule>().IfNotRegistered(typeof(RecaptchaModule));

        _ = builder.RegisterType<SendFeedbackModelValidator>().As<IValidator<SendFeedbackModel>>();
        _ = builder.RegisterType<SendFeedbackHandler>().As<ISendFeedbackHandler>();
        _ = builder.RegisterMapper().SingleInstance();
    }
}
