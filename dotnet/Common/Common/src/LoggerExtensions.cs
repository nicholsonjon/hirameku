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

namespace Hirameku.Common;

using NLog;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

public static class LoggerExtensions
{
    [Conditional("TRACE")]
    public static void Debug<T>(
        this Logger logger,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        T? data = default)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (logger.IsDebugEnabled)
        {
            var source = string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}",
                filePath,
                lineNumber);

            logger.Debug(new
            {
                message,
                memberName,
                source,
                data,
            });
        }
    }

    [Conditional("TRACE")]
    public static void Error<T>(
        this Logger logger,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        T? data = default)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (logger.IsErrorEnabled)
        {
            var source = string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}",
                filePath,
                lineNumber);

            logger.Error(new
            {
                message,
                memberName,
                source,
                data,
            });
        }
    }

    [Conditional("TRACE")]
    public static void Info<T>(
        this Logger logger,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        T? data = default)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (logger.IsInfoEnabled)
        {
            var source = string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}",
                filePath,
                lineNumber);

            logger.Info(new
            {
                message,
                memberName,
                source,
                data,
            });
        }
    }

    [Conditional("DEBUG")]
    public static void Trace<T>(
        this Logger logger,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        T? data = default)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (logger.IsTraceEnabled)
        {
            var source = string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}",
                filePath,
                lineNumber);

            logger.Trace(new
            {
                message,
                memberName,
                source,
                data,
            });
        }
    }

    [Conditional("TRACE")]
    public static void Warn<T>(
        this Logger logger,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        T? data = default)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (logger.IsWarnEnabled)
        {
            var source = string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}",
                filePath,
                lineNumber);

            logger.Warn(new
            {
                message,
                memberName,
                source,
                data,
            });
        }
    }
}
