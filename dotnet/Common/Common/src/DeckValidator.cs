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

public class DeckValidator : AbstractValidator<Deck>
{
    public DeckValidator()
    {
        _ = this.RuleFor(d => d.Cards)
            .Must(d => d.Count() <= Constants.MaxNumberOfCards);
        _ = this.RuleForEach(d => d.Cards)
            .Matches(Regexes.ObjectId);
        _ = this.RuleFor(d => d.Name)
            .NotEmpty()
            .MaximumLength(Constants.MaxStringLengthShort);
        _ = this.RuleFor(d => d.UserId)
            .Matches(Regexes.ObjectId);
    }
}
