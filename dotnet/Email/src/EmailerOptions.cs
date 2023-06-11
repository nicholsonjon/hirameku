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

namespace Hirameku.Email;

public class EmailerOptions
{
    public const string ConfigurationSectionName =
        nameof(Hirameku) + ":" + nameof(Email) + ":" + nameof(EmailerOptions);

    public EmailerOptions()
    {
    }

    public string FeedbackEmailAddress { get; set; } = string.Empty;

    public string QueryStringParameterName { get; set; } = string.Empty;

    public Uri? RejectRegistrationUrl { get; set; }

    public Uri? ResetPasswordUrl { get; set; }

    public string Sender { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public int SmtpPort { get; set; }

    public string SmtpServer { get; set; } = string.Empty;

    public string SmtpUserName { get; set; } = string.Empty;

    public bool UseTls { get; set; }

    public Uri? VerifyEmailUrl { get; set; }
}
