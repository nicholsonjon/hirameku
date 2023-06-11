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

namespace Hirameku.Common.Service;

using Autofac;
using Hirameku.Data;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using System.IO;

public class CommonServiceModule : Module
{
    public CommonServiceModule()
    {
    }

    protected override void Load(ContainerBuilder builder)
    {
        _ = builder.RegisterModule<CommonModule>();
        _ = builder.RegisterModule<DataModule>().IfNotRegistered(typeof(DataModule));

        _ = builder.RegisterType<PasswordValidator>().As<IPasswordValidator>();
        _ = builder.RegisterType<PersistentTokenIssuer>().As<IPersistentTokenIssuer>();
        _ = builder.RegisterType<SecurityTokenIssuer>().As<ISecurityTokenIssuer>();
        _ = builder.Register(
            c =>
            {
                var options = c.Resolve<IOptions<PasswordValidatorOptions>>();
                return new AsyncLazy<IEnumerable<string>>(() => GetPasswordBlacklist(options));
            });
    }

    private static async Task<IEnumerable<string>> GetPasswordBlacklist(IOptions<PasswordValidatorOptions> options)
    {
        using var file = File.OpenRead(options.Value.PasswordBlacklistPath);
        using var reader = new StreamReader(file);
        var blacklistedPasswords = new List<string>();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(line))
            {
                blacklistedPasswords.Add(line);
            }
        }

        return blacklistedPasswords;
    }
}
