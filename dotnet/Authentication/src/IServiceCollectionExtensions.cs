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

using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationConfiguration(
        this IServiceCollection instance,
        IConfiguration configuration)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return instance.AddCachingConfiguration(configuration)
            .AddCommonConfiguration(configuration)
            .AddCommonServiceConfiguration(configuration)
            .AddDataConfiguration(configuration)
            .AddEmailConfiguration(configuration)
            .AddRecaptchaConfiguration(configuration)
            .Configure<AuthenticationOptions>(configuration.GetSection(AuthenticationOptions.ConfigurationSectionName));
    }
}