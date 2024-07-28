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

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hirameku.Contact;
using NLog;
using System.Text.Json;
using System.Text.Json.Serialization;

internal sealed class Program
{
    private static void Main(string[] args)
    {
        var logger = LogManager.Setup().LoadConfigurationFromFile().GetCurrentClassLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureHost(builder);
            ConfigureServices(builder);
            BuildApplication(builder).Run();
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }

    private static WebApplication BuildApplication(WebApplicationBuilder builder)
    {
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            _ = app.UseDeveloperExceptionPage();
            _ = app.UseSwagger();
            _ = app.UseSwaggerUI();
        }
        else
        {
            _ = app.UseExceptionHandler("/error");
        }

        _ = app.UseForwardedHeaders();
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        _ = app.MapControllers();

        return app;
    }

    private static void ConfigureHost(WebApplicationBuilder builder)
    {
        _ = builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(cb => cb.RegisterModule<ContactModule>());
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        _ = services.AddContactConfiguration(configuration);
        _ = services.AddControllers().AddJsonOptions(o =>
        {
            var options = o.JsonSerializerOptions;

            options.Converters.Add(new JsonStringEnumConverter());
            options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        _ = services.AddEndpointsApiExplorer();
        _ = services.AddHttpContextAccessor();
        _ = services.AddApiVersioning();
        _ = services.AddSwaggerGen();
    }
}
