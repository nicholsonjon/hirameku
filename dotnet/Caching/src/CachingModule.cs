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

namespace Hirameku.Caching;

using Autofac;
using Hirameku.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

public class CachingModule : Module
{
    private static readonly object LockObject = new();
    private static ICacheClientFactory? cacheClientFactory;
    private static IDisposable? changeTokenDisposable;

    public CachingModule()
    {
    }

    protected override void Load(ContainerBuilder builder)
    {
        _ = builder.RegisterModule<DataModule>().IfNotRegistered(typeof(DataModule));

        _ = builder.Register(
            c =>
            {
                // you cannot close over the IComponentContext, but we also don't want to Resolve()
                // unless we're sure we need the dependencies--that's why this method is so busy
                if (cacheClientFactory is null)
                {
                    lock (LockObject)
                    {
                        if (cacheClientFactory is null)
                        {
                            cacheClientFactory = new CacheClientFactory(c.Resolve<IOptions<CacheOptions>>());

                            if (changeTokenDisposable is null)
                            {
                                var configuration = c.Resolve<IConfiguration>();

                                changeTokenDisposable = ChangeToken.OnChange(
                                    () => configuration.GetReloadToken(),
                                    () => ConfigurationChanged());
                            }
                        }
                    }
                }

                return cacheClientFactory;
            })
            .ExternallyOwned();
        _ = builder.Register(c => c.Resolve<ICacheClientFactory>().CreateClient());
        _ = builder.RegisterType<CachedValueDao>().As<ICachedValueDao>();
    }

    private static void ConfigurationChanged()
    {
        lock (LockObject)
        {
            cacheClientFactory?.Dispose();
            cacheClientFactory = null;
            changeTokenDisposable?.Dispose();
            changeTokenDisposable = null;
        }
    }
}
