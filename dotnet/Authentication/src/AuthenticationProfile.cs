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

namespace Hirameku.Authentication;

using AutoMapper;
using Hirameku.Common;
using Hirameku.Common.Service;
using Hirameku.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CommonExceptions = Hirameku.Common.Properties.Exceptions;

public class AuthenticationProfile : Profile
{
    public AuthenticationProfile()
    {
        _ = this.CreateMap<AuthenticationData<RenewTokenModel>, AuthenticationEvent>()
            .ForMember(d => d.AuthenticationResult, o => o.Ignore())
            .ForMember(d => d.Hash, o => o.MapFrom(s => Hash(s)))
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.Model.UserId));
        _ = this.CreateMap<AuthenticationData<SignInModel>, AuthenticationEvent>()
            .ForMember(d => d.AuthenticationResult, o => o.Ignore())
            .ForMember(d => d.Hash, o => o.MapFrom(s => Hash(s)))
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore());
        this.CreateMap<PasswordVerificationResult, AuthenticationResult>()
            .ConvertUsing(r => Map(r));
        this.CreateMap<PersistentTokenVerificationResult, AuthenticationResult>()
            .ConvertUsing(r => Map(r));
        _ = this.CreateMap<SignInResult, TokenResponseModel>();
        this.CreateMap<VerificationTokenVerificationResult, ResetPasswordResult>()
            .ConvertUsing(r => Map(r));
    }

    [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "MD5 is employed here to generate a unique key that is not used for any security-critical purposes")]
    private static string Hash<T>(AuthenticationData<T> data)
        where T : class
    {
        const int InitialCapacity = 256;
        var builder = new StringBuilder(InitialCapacity)
            .Append(data.Accept)
            .Append(data.ContentEncoding)
            .Append(data.ContentLanguage)
            .Append(data.RemoteIP)
            .Append(data.UserAgent);

        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static AuthenticationResult Map(PasswordVerificationResult passwordResult)
    {
        return passwordResult switch
        {
            PasswordVerificationResult.NotVerified => AuthenticationResult.NotAuthenticated,
            PasswordVerificationResult.Verified => AuthenticationResult.Authenticated,
            PasswordVerificationResult.VerifiedAndExpired => AuthenticationResult.PasswordExpired,
            _ => throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompositeFormat.Parse(CommonExceptions.InvalidEnumValue).Format,
                    passwordResult,
                    typeof(PasswordVerificationResult))),
        };
    }

    private static AuthenticationResult Map(PersistentTokenVerificationResult result)
    {
        return result switch
        {
            PersistentTokenVerificationResult.NoTokenAvailable => AuthenticationResult.NotAuthenticated,
            PersistentTokenVerificationResult.NotVerified => AuthenticationResult.NotAuthenticated,
            PersistentTokenVerificationResult.Verified => AuthenticationResult.Authenticated,
            _ => throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompositeFormat.Parse(CommonExceptions.InvalidEnumValue).Format,
                    result,
                    typeof(PersistentTokenVerificationResult))),
        };
    }

    private static ResetPasswordResult Map(VerificationTokenVerificationResult result)
    {
        return result switch
        {
            VerificationTokenVerificationResult.NotVerified => ResetPasswordResult.TokenNotVerified,
            VerificationTokenVerificationResult.TokenExpired => ResetPasswordResult.TokenExpired,
            VerificationTokenVerificationResult.Verified => ResetPasswordResult.PasswordReset,
            _ => throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompositeFormat.Parse(CommonExceptions.InvalidEnumValue).Format,
                    result,
                    typeof(VerificationTokenVerificationResult))),
        };
    }
}
