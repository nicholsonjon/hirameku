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

import React from "react";
import type { HTMLInputTypeAttribute } from "react";
import type { FieldError, FieldValues, UseFormRegister } from "react-hook-form";

type Attributes = {
  autoComplete?: string;
  className: string;
  id: string;
  maxLength?: number;
  minLength?: number;
  name: string;
  required: boolean;  
  pattern?: RegExp;
  placeholder?: string;
};

type Input = Attributes & Validatable & {
  type: HTMLInputTypeAttribute
};

type TextArea = Attributes & Validatable & {
  rows?: number;
  type: "textarea";
};

type Validatable = {
  errors: { [key: string]: FieldError | undefined };
  isDirty: boolean;
  register: UseFormRegister<FieldValues>;
  t: (key: string, options?: { [key: string]: number }) => string;
};

function getAriaDescribedBy(id: string, tooltipId?: string | undefined): string {
  let ariaDescribedBy = id + "-help";

  if (tooltipId) {
    ariaDescribedBy + " " + tooltipId;
  }

  return ariaDescribedBy;
}

function getAriaErrorMessageId(id: string): string {
  return `${id}-errors`;
}

function getClassName(isInvalid: boolean): string {
  return isInvalid ? "form-control is-invalid" : "form-control";
}

function getTooltipId(id: string): string {
  return id + "-tooltip";
}

function getValidationRules(props: Input | TextArea) {
  const maxLength = props.maxLength;
  const minLength = props.minLength;
  const pattern = props.pattern;
  const required = props.required
  const i18nKey = props.name;
  const t = props.t;

  return {
    ...(maxLength && {
      maxLength: {
        value: maxLength,
        message: t(`${i18nKey}.maxLength`, { val: maxLength }),
      }
    }),
    ...(minLength && {
      minLength: {
        value: minLength,
        message: t(`${i18nKey}.minLength`, { val: minLength }),
      }
    }),
    ...(pattern && {
      pattern: {
        value: pattern,
        message: t(`${i18nKey}.pattern`),
      }
    }),
    ...(required && {
      required: {
        value: required,
        message: t(`${i18nKey}.required`),
      },
    }),
  };
}

function hasErrors(props: Input | TextArea): boolean {
  return !!props.errors[props.name];
}

function Input(props: Input & { tooltip?: string | undefined }) {
  const id = props.id;
  const placeholder = props.placeholder;
  const isInvalid = hasErrors(props);

  return (
    <input
      {...props.register(props.name, getValidationRules(props))}
      type={props.type}
      id={props.id}
      className={getClassName(isInvalid)}
      placeholder={placeholder}
      aria-describedby={getAriaDescribedBy(id, props.tooltip && getTooltipId(id))}
      aria-errormessage={getAriaErrorMessageId(id)}
      aria-invalid={isInvalid}
      aria-placeholder={placeholder}
      autoComplete={props.autoComplete} />
  );
}

function Label(props: { id: string, text: string, tooltip?: string | undefined }) {
  const tooltip = props.tooltip;

  return (
    <label htmlFor={props.id} className="form-label">
      {props.text}
      {
        tooltip
        ? <span data-bs-toggle="tooltip" data-bs-title={tooltip}>
            &nbsp;<i className="bi bi-question-circle"></i>
          </span>
        : null
      }
    </label>
  );
}

function TextArea(props: TextArea & { tooltip?: string | undefined }) {
  const id = props.id;
  const placeholder = props.placeholder;
  const isInvalid = hasErrors(props);

  return (
    <textarea
      {...props.register(props.name, getValidationRules(props))}
      id={id}
      className={getClassName(isInvalid)}
      placeholder={placeholder}
      rows={props.rows}
      aria-describedby={getAriaDescribedBy(id, props.tooltip && getTooltipId(id))}
      aria-errormessage={getAriaErrorMessageId(id)}
      aria-invalid={isInvalid}
      aria-placeholder={placeholder}
      autoComplete={props.autoComplete}></textarea>
  );
}

export default function ValidatedInput(props: Input | TextArea) {
  const i18nKey = props.name;
  const t = props.t;
  const tooltipKey = `${i18nKey}.tooltip`;
  const tooltipText = t(tooltipKey);
  const tooltip = tooltipText !== tooltipKey ? tooltipText : undefined;
  let input: React.JSX.Element;

  if (props.type === "textarea") {
    const textAreaProps = props as TextArea;
    const p = { tooltip, ...textAreaProps };
    input = <TextArea {...p} />;
  } else {
    const inputProps = props as Input;
    const p = { tooltip, ...inputProps };
    input = <Input {...p} />;
  }

  const id = props.id;
  const helpText = t(`${i18nKey}.helpText`);
  const error = props.errors[props.name];

  return (
    <div className={props.className}>
      <Label id={id} text={t(`${i18nKey}.label`)} tooltip={tooltip} />
      {tooltip && <span id={getTooltipId(id)} className="visually-hidden">{tooltip}</span>}
      {input}
      {!props.isDirty && !error && helpText && <div id={getAriaDescribedBy(id)} className="form-text">{helpText}</div>}
      {error && <div id={getAriaErrorMessageId(props.id)} className="invalid-feedback" role="alert">{error.message}</div>}
    </div>
  );
}
