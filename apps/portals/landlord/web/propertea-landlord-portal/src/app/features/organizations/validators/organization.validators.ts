import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function passwordMatchValidator(passwordFieldName = 'userPassword', confirmFieldName = 'confirmPassword'): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const password = control.get(passwordFieldName);
    const confirmPassword = control.get(confirmFieldName);

    if (!password || !confirmPassword) {
      return null;
    }

    return password.value === confirmPassword.value ? null : { passwordMismatch: true };
  };
}

export function passwordComplexityValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;

    if (!value) {
      return null;
    }

    const errors: ValidationErrors = {};

    if (!/(?=.*[a-z])/.test(value)) {
      errors['passwordMissingLowercase'] = true;
    }

    if (!/(?=.*[A-Z])/.test(value)) {
      errors['passwordMissingUppercase'] = true;
    }

    if (!/(?=.*\d)/.test(value)) {
      errors['passwordMissingNumber'] = true;
    }

    if (!/(?=.*[^A-Za-z0-9])/.test(value)) {
      errors['passwordMissingSpecial'] = true;
    }

    return Object.keys(errors).length > 0 ? errors : null;
  };
}

export function organizationNameValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;

    if (!value) {
      return null;
    }

    const invalidChars = /[<>:"\/\\|?*]/;
    if (invalidChars.test(value)) {
      return { invalidOrganizationName: true };
    }

    return null;
  };
}
