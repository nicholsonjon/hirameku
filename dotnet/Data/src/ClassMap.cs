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

using Hirameku.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

public static class ClassMap
{
    private static readonly object LockObject = new();
    private static bool areClassMapsRegistered;

    public static void RegisterClassMaps()
    {
        lock (LockObject)
        {
            if (!areClassMapsRegistered)
            {
                _ = BsonClassMap.RegisterClassMap<AuthenticationEvent>(
                    cm =>
                    {
                        cm.AutoMap();
                        _ = cm.MapMember(c => c.AuthenticationResult)
                            .SetSerializer(new EnumSerializer<AuthenticationResult>(BsonType.String));
                        _ = cm.MapMember(c => c.UserId).SetElementName("user_id");
                    });
                _ = BsonClassMap.RegisterClassMap<Card>(
                    cm =>
                    {
                        cm.AutoMap();
                        _ = cm.MapMember(c => c.CreationDate).SetSerializer(new DateTimeSerializer(BsonType.Document));
                        cm.SetIsRootClass(true);
                    });
                _ = BsonClassMap.RegisterClassMap<CardDocument>(
                    cm =>
                    {
                        cm.AutoMap();
                        _ = cm.MapIdMember(c => c.Id);
                    });
                _ = BsonClassMap.RegisterClassMap<Deck>(
                    cm =>
                    {
                        cm.AutoMap();
                        _ = cm.MapMember(d => d.CreationDate).SetSerializer(new DateTimeSerializer(BsonType.Document));
                        _ = cm.MapMember(d => d.UserId).SetElementName("user_id");
                        cm.SetIsRootClass(true);
                    });
                _ = BsonClassMap.RegisterClassMap<DeckDocument>(
                    cm =>
                    {
                        cm.AutoMap();
                        _ = cm.MapIdMember(c => c.Id);
                    });
                _ = BsonClassMap.RegisterClassMap<Meaning>(cm => cm.AutoMap());
                _ = BsonClassMap.RegisterClassMap<PasswordHash>(
                    cm =>
                    {
                        cm.AutoMap();
                        _ = cm.MapMember(ph => ph.ExpirationDate)
                            .SetIgnoreIfNull(true)
                            .SetSerializer(new NullableSerializer<DateTime>(new DateTimeSerializer(BsonType.Document)));
                        _ = cm.MapMember(ph => ph.LastChangeDate).SetSerializer(new DateTimeSerializer(BsonType.Document));
                        _ = cm.MapMember(ph => ph.Version).SetSerializer(new PasswordHashVersionSerializer());
                    });
                _ = BsonClassMap.RegisterClassMap<PersistentToken>(
                    cm =>
                    {
                        cm.AutoMap();
                        _ = cm.MapMember(pt => pt.ClientId).SetElementName("client_id");
                        _ = cm.MapMember(pt => pt.ExpirationDate).SetSerializer(new DateTimeSerializer(BsonType.Document));
                    });
                _ = BsonClassMap.RegisterClassMap<Review>(
                    cm =>
                    {
                        cm.AutoMap();
                        _ = cm.MapMember(c => c.Disposition).SetSerializer(new EnumSerializer<Disposition>(BsonType.String));
                        _ = cm.MapMember(c => c.Interval).SetSerializer(new EnumSerializer<Interval>(BsonType.String));
                        _ = cm.MapMember(r => r.ReviewDate).SetSerializer(new DateTimeSerializer(BsonType.Document));
                        cm.SetIsRootClass(true);
                    });
                _ = BsonClassMap.RegisterClassMap<ReviewDocument>(
                    cm =>
                    {
                        cm.AutoMap();
                        _ = cm.MapIdMember(c => c.Id);
                    });
                _ = BsonClassMap.RegisterClassMap<User>(cm =>
                {
                    cm.AutoMap();
                    _ = cm.MapMember(c => c.UserStatus).SetSerializer(new EnumSerializer<UserStatus>(BsonType.String));
                    cm.SetIsRootClass(true);
                });
                _ = BsonClassMap.RegisterClassMap<UserDocument>(
                    cm =>
                    {
                        cm.AutoMap();
                        _ = cm.MapIdField(u => u.Id);
                        _ = cm.MapMember(u => u.PasswordHash);
                        _ = cm.MapMember(u => u.PersistentTokens);
                    });
                _ = BsonClassMap.RegisterClassMap<Verification>(
                    cm =>
                    {
                        cm.AutoMap();
                        _ = cm.MapIdField(v => v.Id);
                        _ = cm.MapMember(v => v.CreationDate).SetSerializer(new DateTimeSerializer(BsonType.Document));
                        _ = cm.MapMember(v => v.ExpirationDate)
                            .SetIgnoreIfNull(true)
                            .SetSerializer(new NullableSerializer<DateTime>(new DateTimeSerializer(BsonType.Document)));
                        _ = cm.MapMember(v => v.Type).SetSerializer(new EnumSerializer<VerificationType>(BsonType.String));
                        _ = cm.MapMember(v => v.UserId).SetElementName("user_id");
                    });

                var conventionPack = new ConventionPack { new CamelCaseElementNameConvention() };
                ConventionRegistry.Register("camelCase", conventionPack, _ => true);

                areClassMapsRegistered = true;
            }
        }
    }
}
