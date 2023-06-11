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
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using NLog;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommonExceptions = Hirameku.Common.Properties.Exceptions;

/// <remarks>
/// This implementation is based on <see cref="Microsoft.Extensions.Identity.Core.PasswordHasher{TUser}"/>, which is
/// copyright (c) .NET Foundation and licensed under the Apache License Version 2.0. We're not using Microsoft's
/// implementation because it's behind OWASP recommendations.
/// </remarks>
public class PasswordHasher : IPasswordHasher
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public PasswordHasher(PasswordHashVersion? currentVersion = default)
    {
        this.CurrentVersion = currentVersion ?? PasswordHashVersion.Current;
    }

    private PasswordHashVersion CurrentVersion { get; }

    public HashPasswordResult HashPassword(string password)
    {
        Log.Trace("Entering method", data: new { parameters = new { password = "REDACTED" } });

        var version = this.CurrentVersion;

        return this.HashPassword(password, RandomNumberGenerator.GetBytes(version.SaltLength), version);
    }

    public HashPasswordResult HashPassword(string password, byte[] salt)
    {
        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new
                {
                    password = "REDACTED",
                    salt = "REDACTED",
                },
            });

        return this.HashPassword(password, salt, this.CurrentVersion);
    }

    public HashPasswordResult HashPassword(string password, PasswordHashVersion version)
    {
        Log.Trace("Entering method", data: new { parameters = new { password = "REDACTED", version } });

        if (version == null)
        {
            throw new ArgumentNullException(nameof(version));
        }

        return this.HashPassword(password, RandomNumberGenerator.GetBytes(version.SaltLength), version);
    }

    public HashPasswordResult HashPassword(string password, byte[] salt, PasswordHashVersion version)
    {
        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new
                {
                    password = "REDACTED",
                    salt = "REDACTED",
                    version,
                },
            });

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(CommonExceptions.StringNullOrWhiteSpace, nameof(password));
        }

        if (salt == null)
        {
            throw new ArgumentNullException(nameof(salt));
        }

        if (version == null)
        {
            throw new ArgumentNullException(nameof(version));
        }

        var hash = KeyDerivation.Pbkdf2(
            password,
            salt,
            version.KeyDerivationPrf,
            version.Iterations,
            version.KeyLength);

        Log.Trace("Exiting method", data: new { returnValue = "REDACTED" });

        return new HashPasswordResult(hash, salt, version);
    }

    public VerifyPasswordResult VerifyPassword(
        PasswordHashVersion version,
        byte[] salt,
        byte[] hashedPassword,
        string password)
    {
        Log.Trace(
            "Entering method",
            data: new
            {
                parameters = new
                {
                    version,
                    salt = "REDACTED",
                    hashedPassword = "REDACTED",
                    password = "REDACTED",
                },
            });

        if (version == null)
        {
            throw new ArgumentNullException(nameof(version));
        }

        var hashPasswordResult = this.HashPassword(password, salt, version);
        var areEqual = AreEqual(hashedPassword, hashPasswordResult.Hash);
        var result = areEqual
            ? this.CurrentVersion.Name == version.Name
                ? VerifyPasswordResult.Verified
                : VerifyPasswordResult.VerifiedAndRehashRequired
            : VerifyPasswordResult.NotVerified;

        Log.Trace("Exiting method", data: new { returnValue = result });

        return result;
    }

    // this method is intentionally not optimized or inlined
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static bool AreEqual(byte[] a, byte[] b)
    {
        if (a == null && b == null)
        {
            return true;
        }

        if (a == null || b == null || a.Length != b.Length)
        {
            return false;
        }

        var equal = true;

        // this loop is intentionally written to consistently consume the same amount of time for any given pair of hashes
        for (var i = 0; i < a.Length; i++)
        {
            equal &= a[i] == b[i];
        }

        return equal;
    }
}
