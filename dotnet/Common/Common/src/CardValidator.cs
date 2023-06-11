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

using FluentValidation;
using System.Text.RegularExpressions;

public class CardValidator : AbstractValidator<Card>
{
    public CardValidator()
    {
        _ = this.RuleFor(c => c.Expression)
            .NotEmpty()
            .MaximumLength(Constants.MaxStringLengthShort);
        _ = this.RuleFor(c => c.Meanings)
            .NotEmpty()
            .Must(m => m.Count() <= Constants.MaxNumberOfMeanings);
        _ = this.RuleForEach(c => c.Meanings).SetValidator(new MeaningValidator());
        _ = this.RuleFor(f => f.Notes)
            .MaximumLength(Constants.MaxStringLengthLong)
            .Must(s => !Regex.IsMatch(s, Regexes.EntirelyWhiteSpace));
        _ = this.RuleFor(f => f.Reading)
            .MaximumLength(Constants.MaxStringLengthShort)
            .Must(s => !Regex.IsMatch(s, Regexes.EntirelyWhiteSpace));
    }
}
