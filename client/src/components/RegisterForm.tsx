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

<section>
  <form id="registration-form">
    <div className="row mb-3">
      <label htmlFor="email-address" className="form-label">Email Address:</label>
      <input type="email" className="form-control" name="emailAddress" aria-describedby="email-help" />
      <span id="email-help"
        data-bs-toggle="tooltip"
        title="Your email address will only be used for account administration purposes. Your data are never shared or sold.">
        <i className="bi bi-question-circle"></i>
      </span>
    </div>
    <div className="row mb-3">
      <label htmlFor="username" className="form-label">Username:</label>
      <input type="text" className="form-control" name="username" aria-describedby="username-help" />
      <span id="username-help" data-bs-toggle="tooltip" title="Your username is used to uniquely identify your account.">
        <i className="bi bi-question-circle"></i>
      </span>
    </div>
    <div className="row mb-3">
      <label htmlFor="name" className="form-label">Name:</label>
      <input type="text" className="form-control" name="name" aria-describedby="name-help" />
      <span id="name-help"
        data-bs-toggle="tooltip"
        title="You may use whatever name you prefer, as long as it does not violate the terms of service.">
        <i className="bi bi-question-circle"></i>
      </span>
    </div>
    <div className="row mb-3">
      <label htmlFor="password" className="form-label">Password:</label>
      <input type="password" className="form-control" name="password" aria-describedby="password-help" />
      <span id="password-help"
        data-bs-toggle="tooltip"
        title="Consider using a long, memorable passphrase instead of a password. Not only will it be easier to remember, but long passphrases are more secure. Even better, use a password manager to generate a strong, random password for you.">
        <i className="bi bi-question-circle"></i>
      </span>
    </div>
    <div className="row mb-3">
      <label htmlFor="confirm-password" className="form-label">Confirm Password:</label>
      <input type="password" className="form-control" name="confirmPassword" aria-describedby="confirm-password-help" />
      <span id="confirm-password-help"
        data-bs-toggle="tooltip"
        title="Please enter your password again to ensure there are no typographical errors.">
        <i className="bi bi-question-circle"></i>
      </span>
    </div>
    <div className="row mb-3">
      <button className="btn btn-primary" type="submit">Register</button>
    </div>
  </form>
</section>
