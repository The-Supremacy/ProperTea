import { Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { OrganizationService } from '../../services/organization.service';
import { AuthService } from '../../../../auth/services/auth.service';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { FormFieldComponent } from '../../../../shared/components';

@Component({
  selector: 'app-register-organization',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    CardModule,
    InputTextModule,
    ButtonModule,
    MessageModule,
    ProgressSpinnerModule,
    TranslocoModule,
    FormFieldComponent
  ],
  templateUrl: './register-organization.component.html',
  styleUrl: './register-organization.component.scss'
})
export class RegisterOrganizationComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly orgService = inject(OrganizationService);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly translocoService = inject(TranslocoService);

  registrationForm: FormGroup;
  isSubmitting = signal(false);
  errorMessage = signal<string | null>(null);
  nameAvailabilityChecking = signal(false);
  nameAvailabilityMessage = signal<{ text: string; available: boolean } | null>(null);

  constructor() {
    this.registrationForm = this.fb.group({
      organizationName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      userEmail: ['', [Validators.required, Validators.email]],
      userFirstName: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(100)]],
      userLastName: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(100)]],
      userPassword: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(100),
        Validators.pattern(/(?=.*[a-z])/), // lowercase
        Validators.pattern(/(?=.*[A-Z])/), // uppercase
        Validators.pattern(/(?=.*\d)/),    // number
        Validators.pattern(/(?=.*[^A-Za-z0-9])/) // symbol
      ]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  // Custom validator to check if passwords match
  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('userPassword');
    const confirmPassword = control.get('confirmPassword');

    if (!password || !confirmPassword) {
      return null;
    }

    return password.value === confirmPassword.value ? null : { passwordMismatch: true };
  }

  ngOnInit(): void {
    // Setup organization name availability check
    this.registrationForm.get('organizationName')?.valueChanges.pipe(
      debounceTime(500),
      distinctUntilChanged(),
      switchMap(name => {
        if (name && name.length >= 2) {
          this.nameAvailabilityChecking.set(true);
          this.nameAvailabilityMessage.set(null);
          return this.orgService.checkAvailability(name);
        }
        this.nameAvailabilityMessage.set(null);
        return [];
      })
    ).subscribe({
      next: (response) => {
        this.nameAvailabilityChecking.set(false);
        this.nameAvailabilityMessage.set({
          text: response.nameAvailable
            ? this.translocoService.translate('register.availability.available')
            : this.translocoService.translate('register.availability.taken'),
          available: response.nameAvailable
        });
      },
      error: () => {
        this.nameAvailabilityChecking.set(false);
        this.nameAvailabilityMessage.set(null);
      }
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.registrationForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getPasswordError(): string | null {
    const passwordControl = this.registrationForm.get('userPassword');
    if (!passwordControl || !passwordControl.errors || !passwordControl.touched) {
      return null;
    }

    if (passwordControl.errors['required']) {
      return this.translocoService.translate('register.field.error.password.required');
    }
    if (passwordControl.errors['minlength']) {
      return this.translocoService.translate('register.field.error.password.minlength');
    }
    if (passwordControl.errors['pattern']) {
      // Check which pattern failed
      const value = passwordControl.value || '';
      if (!/[a-z]/.test(value)) {
        return this.translocoService.translate('register.field.error.password.lowercase');
      }
      if (!/[A-Z]/.test(value)) {
        return this.translocoService.translate('register.field.error.password.uppercase');
      }
      if (!/\d/.test(value)) {
        return this.translocoService.translate('register.field.error.password.number');
      }
      if (!/[^A-Za-z0-9]/.test(value)) {
        return this.translocoService.translate('register.field.error.password.symbol');
      }
    }
    return null;
  }

  getConfirmPasswordError(): string | null {
    const confirmControl = this.registrationForm.get('confirmPassword');
    if (!confirmControl || !confirmControl.touched) {
      return null;
    }

    if (confirmControl.errors?.['required']) {
      return this.translocoService.translate('register.field.error.confirmPassword.required');
    }

    if (this.registrationForm.errors?.['passwordMismatch'] && confirmControl.dirty) {
      return this.translocoService.translate('register.field.error.confirmPassword.mismatch');
    }

    return null;
  }

  canSubmit(): boolean {
    const nameAvailable = this.nameAvailabilityMessage();
    return this.registrationForm.valid &&
           !this.isSubmitting() &&
           (!nameAvailable || nameAvailable.available);
  }

  onSubmit(): void {
    if (!this.canSubmit()) {
      Object.keys(this.registrationForm.controls).forEach(key => {
        this.registrationForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.orgService.registerOrganization(this.registrationForm.value).subscribe({
      next: () => {
        this.authService.login();
      },
      error: (error) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(
          error.error?.detail || error.error?.title || this.translocoService.translate('register.error.generic')
        );
      }
    });
  }

  signIn(): void {
    this.authService.login();
  }
}
