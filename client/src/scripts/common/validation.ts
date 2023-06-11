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

export const Validation = {
    emailAddress: {
        maxLength: 253,
        minLength: 6,
        pattern: /[0-9A-Za-z.!#$%&'*+\/=?^_`{|}~-]+@(?!-)(?:(?:[a-zA-Z0-9][a-zA-Z0-9\-]{0,61})?[a-zA-Z0-9]\.){1,126}(?!0-9+)[a-zA-Z0-9]{2,63}/u,
    },
    feedback: {
        maxLength: 4000,
        minLength: 3,
        pattern: /(?:\S+(?:\s*\S+)*)+/u,
    },
    name: {
        maxLength: 40,
        minLength: 1,
        pattern: /\p{L}+(?:\s*\p{L}+)+/u,
    },
    userName: {
        maxLength: 32,
        minLength: 4,
        pattern: /[0-9A-Za-z\-._~!*]+/u,
    }
};
