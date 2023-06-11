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

import i18n from "i18next";
import { initReactI18next, useTranslation } from "react-i18next";
import { type FieldError, type FieldValues, useForm, type UseFormRegister } from "react-hook-form";
import ValidatedInput from "./ValidatedInput";
import { Validation } from  "../scripts/common/validation";

i18n.use(initReactI18next).init();

const nameValidation = Validation.name;
const emailValidation = Validation.emailAddress;
const feedbackValidation = Validation.feedback;

export default function ContactForm() {
  const { formState: { dirtyFields, errors }, handleSubmit, register } = useForm({
    criteriaMode: "all",
    defaultValues: {
      "emailAddress": "",
      "feedback": "",
      "name": "",
    },
  });
  const { t } = useTranslation("contact-form");
  const submit = (data: FieldValues) => {
    console.log(data);
  };

  // the design of the FieldErrors<T> type makes it impossible to dynamically access the properties of the errors object, so we need to cast
  // in order to maintain separation between ValidatedInput and the form
  const errorList = errors as { [key: string]: FieldError | undefined };

  // and for the same reason, we also need to cast register
  const doRegister = register as unknown as UseFormRegister<FieldValues>;

  return (
    <form id="contact-form" className="needs-validation" noValidate onSubmit={handleSubmit(submit)}>
      <div className="row mb-3">
        <ValidatedInput
          autoComplete="name"
          className="col-6"
          errors={errorList}
          id="name"
          isDirty={dirtyFields.name || false}
          maxLength={nameValidation.maxLength}
          name="name"
          pattern={nameValidation.pattern}
          register={doRegister}
          required
          t={t}
          type="text" />
        <ValidatedInput
          autoComplete="email"
          className="col-6"
          errors={errorList}
          id="email-address"
          isDirty={dirtyFields.emailAddress || false}
          maxLength={emailValidation.maxLength}
          minLength={emailValidation.minLength}
          name="emailAddress"
          pattern={emailValidation.pattern}
          placeholder="username@hostname.tld"
          register={doRegister}
          required
          t={t}
          type="email" />
      </div>
      <div className="row mb-3">
        <ValidatedInput
          className="col-12"
          errors={errorList}
          id="feedback"
          isDirty={dirtyFields.feedback || false}
          maxLength={feedbackValidation.maxLength}
          minLength={feedbackValidation.minLength}
          name="feedback"
          pattern={feedbackValidation.pattern}
          register={doRegister}
          required
          rows={5}
          t={t}
          type="textarea" />
      </div>
      <div className="row mb-3">
        <div className="d-flex justify-content-end">
          <button type="submit" id="submit" className="btn btn-primary">{t("submitButtonName")}</button>
        </div>
      </div>
    </form>
  );
}
