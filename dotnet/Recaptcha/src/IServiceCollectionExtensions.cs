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

namespace Hirameku.Recaptcha;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddRecaptchaConfiguration(
        this IServiceCollection instance,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(configuration);

        var configurationSection = configuration.GetSection(RecaptchaOptions.ConfigurationSectionName);

        if (!TimeSpan.TryParse(
            configurationSection[nameof(RecaptchaOptions.MedianFirstRetryDelay)],
            out var medianFirstRetryDelay))
        {
            medianFirstRetryDelay = TimeSpan.FromSeconds(1);
        }

        if (!int.TryParse(configurationSection[nameof(RecaptchaOptions.MaxRetries)], out var maxRetries))
        {
            maxRetries = 5;
        }

        _ = instance.AddHttpClient<IRecaptchaClient, RecaptchaClient>()
            .AddTransientHttpErrorPolicy(
                pb => pb.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay, maxRetries)));

        return instance.Configure<RecaptchaOptions>(configurationSection);
    }
}
