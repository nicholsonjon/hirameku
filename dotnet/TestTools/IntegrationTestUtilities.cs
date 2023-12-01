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

namespace Hirameku.TestTools;

using Autofac;
using Autofac.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

public static class IntegrationTestUtilities
{
    public static async Task<dynamic> GetAppSettings(string filename)
    {
        using var stream = File.OpenRead(filename);
        using var reader = new StreamReader(stream);
        dynamic? appSettings = JsonConvert.DeserializeObject(await reader.ReadToEndAsync().ConfigureAwait(false));

        if (appSettings == null)
        {
            Assert.Fail($"Unable to deserialize ${filename} file");
        }

        return appSettings;
    }

    public static async Task ModifyAppSettingsFile(string filename, Action<dynamic> modifyAction)
    {
        var appSettings = await GetAppSettings(filename).ConfigureAwait(false);

        modifyAction(appSettings);

        await File.WriteAllTextAsync(filename, JsonConvert.SerializeObject(appSettings))
            .ConfigureAwait(false);
    }

    public static async Task RunConfigurationChangeTrackingTest<TModule, TService, TOptions>(
        IOptions<TOptions> options,
        Func<Task> initializeConfigFile,
        Func<Task> modifyConfigFile)
        where TModule : IModule, new()
        where TService : notnull
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(initializeConfigFile);
        ArgumentNullException.ThrowIfNull(modifyConfigFile);

        await initializeConfigFile().ConfigureAwait(false);

        var builder = new ContainerBuilder();
        _ = builder.RegisterModule<TModule>();
        _ = builder.RegisterInstance(options);
        var mockDisposable = new Mock<IDisposable>();
        mockDisposable.Setup(m => m.Dispose())
            .Verifiable();
        var mockChangeToken = new Mock<IChangeToken>();
        Action<object>? changeCallback = default;
        object? state = default;
        _ = mockChangeToken.Setup(m => m.RegisterChangeCallback(It.IsAny<Action<object?>>(), It.IsAny<object>()))
            .Callback<Action<object>, object>(
                (cb, s) =>
                {
                    changeCallback = cb;
                    state = s;
                })
            .Returns(mockDisposable.Object);
        var mockConfiguration = new Mock<IConfiguration>();
        _ = mockConfiguration.Setup(m => m.GetReloadToken())
            .Returns(mockChangeToken.Object);
        var configuration = mockConfiguration.Object;
        _ = builder.RegisterInstance(configuration);
        var container = builder.Build();

        var firstFactory = container.Resolve<TService>();

        await modifyConfigFile().ConfigureAwait(false);

        Assert.IsNotNull(changeCallback);
        Assert.IsNotNull(state);
        changeCallback!(state!);

        var secondFactory = container.Resolve<TService>();

        // We invoke the callback again to trigger the ConfigurationChanged() listener in DataModule, so that the
        // singletons will be disposed and nulled out. This is necessary to prevent subsequent integration tests
        // from failing due to the guard clauses in the registration delegates checking for these singletons being null.
        changeCallback(state!);

        Assert.AreNotEqual(firstFactory, secondFactory);
        mockDisposable.Verify();
    }
}
