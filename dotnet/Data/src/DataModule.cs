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

namespace Hirameku.Data;

using Autofac;
using FluentValidation;
using Hirameku.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MongoDB.Driver;

public class DataModule : Module
{
    private static readonly object LockObject = new();
    private static IDisposable? changeTokenDisposable;
    private static IMongoClient? mongoClient;
    private static IMongoDatabase? mongoDatabase;

    public DataModule()
    {
    }

    protected override void Load(ContainerBuilder builder)
    {
        _ = builder.RegisterModule<CommonModule>().IfNotRegistered(typeof(CommonModule));

        ConfigureDatabase(builder);

        _ = builder.RegisterType<AuthenticationEventValidator>().As<IValidator<AuthenticationEvent>>();
        _ = builder.RegisterType<CardDocumentValidator>().As<IValidator<CardDocument>>();
        _ = builder.RegisterType<DeckDocumentValidator>().As<IValidator<DeckDocument>>();
        _ = builder.RegisterType<PasswordHasher>().As<IPasswordHasher>();
        _ = builder.RegisterType<ReviewDocumentValidator>().As<IValidator<ReviewDocument>>();
        _ = builder.RegisterType<UserDocumentValidator>().As<IValidator<UserDocument>>();
    }

    private static void ConfigurationChanged()
    {
        lock (LockObject)
        {
            mongoDatabase = null;
            mongoClient = null;
            changeTokenDisposable?.Dispose();
            changeTokenDisposable = null;
        }
    }

    private static void ConfigureDatabase(ContainerBuilder builder)
    {
        _ = builder.Register(
            c =>
            {
                // you cannot close over the IComponentContext, but we also don't want to Resolve()
                // unless we're sure we need the dependencies--that's why this method is so busy
                if (mongoClient is null)
                {
                    lock (LockObject)
                    {
                        if (mongoClient is null)
                        {
                            mongoClient = new MongoClient(
                                c.Resolve<IOptions<DatabaseOptions>>().Value.ConnectionString);

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

                return mongoClient;
            })
            .ExternallyOwned();
        _ = builder.Register(
            c =>
            {
                // you cannot close over the IComponentContext, but we also don't want to Resolve()
                // unless we're sure we need the dependencies--that's why this method is so busy
                if (mongoDatabase is null)
                {
                    lock (LockObject)
                    {
                        if (mongoDatabase is null)
                        {
                            mongoDatabase = c.Resolve<IMongoClient>().GetDatabase(
                                c.Resolve<IOptions<DatabaseOptions>>().Value.DatabaseName);

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

                return mongoDatabase;
            })
            .ExternallyOwned();

        static IMongoCollection<TDocument> GetCollection<TDocument>(
            IComponentContext context,
            string collectionName)
        {
            return context.Resolve<IMongoDatabase>().GetCollection<TDocument>(collectionName);
        }

        _ = builder.Register(c => GetCollection<AuthenticationEvent>(c, AuthenticationEvent.CollectionName));
        _ = builder.Register(c => GetCollection<CardDocument>(c, CardDocument.CollectionName));
        _ = builder.Register(c => GetCollection<DeckDocument>(c, DeckDocument.CollectionName));
        _ = builder.Register(c => GetCollection<ReviewDocument>(c, ReviewDocument.CollectionName));
        _ = builder.Register(c => GetCollection<UserDocument>(c, UserDocument.CollectionName));
        _ = builder.Register(c => GetCollection<Verification>(c, Verification.CollectionName));
        _ = builder.RegisterType<DocumentDao<AuthenticationEvent>>().As<IDocumentDao<AuthenticationEvent>>();
        _ = builder.RegisterType<DocumentDao<CardDocument>>().As<IDocumentDao<CardDocument>>();
        _ = builder.RegisterType<DocumentDao<DeckDocument>>().As<IDocumentDao<DeckDocument>>();
        _ = builder.RegisterType<DocumentDao<ReviewDocument>>().As<IDocumentDao<ReviewDocument>>();
        _ = builder.RegisterType<DocumentDao<UserDocument>>().As<IDocumentDao<UserDocument>>();
        _ = builder.RegisterType<PasswordDao>().As<IPasswordDao>();
        _ = builder.RegisterType<PersistentTokenDao>().As<IPersistentTokenDao>();
        _ = builder.RegisterType<VerificationDao>().As<IVerificationDao>();

        ClassMap.RegisterClassMaps();
    }
}
