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
// using System;

namespace Hirameku.Registration;

using AutoMapper;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Common.Service;
using NLog;

public class ValidatePasswordHandler : IValidatePasswordHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ValidatePasswordHandler(IMapper mapper, IPasswordValidator passwordValidator)
    {
        this.Mapper = mapper;
        this.PasswordValidator = passwordValidator;
    }

    private IMapper Mapper { get; }

    private IPasswordValidator PasswordValidator { get; }

    public async Task<PasswordValidationResult> ValidatePassword(
        string password,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { password = "REDACTED", cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        var result = await this.PasswordValidator.Validate(password, cancellationToken)
            .ConfigureAwait(false);
        var mappedResult = this.Mapper.Map<PasswordValidationResult>(result);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, mappedResult)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return mappedResult;
    }
}
