import { AbstractControl } from "@angular/forms";
import { FormlyFieldConfig } from "@ngx-formly/core";

export function minlengthValidationMessages(_error: unknown, field: FormlyFieldConfig) {
    return `Should have at least ${field.props?.minLength} characters`;
  }

  export function fieldMatchValidator(control: AbstractControl) {
    const { password, passwordConfirm } = control.value;

    // avoid displaying the message error when values are empty
    if (!passwordConfirm || !password) {
      return null;
    }

    if (passwordConfirm === password) {
      return null;
    }

    return { fieldMatch: { message: 'Password Not Matching' } };
  }
