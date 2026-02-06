import { Component, inject, signal, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { Router } from '@angular/router';
import {
  debounceTime,
  distinctUntilChanged,
  of,
  Subject,
  takeUntil,
  catchError,
} from 'rxjs';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { SessionService } from '../../../../core/services/session.service';
import { OrganizationService } from '../../services/organization.service';
import { ToastService } from '../../../../core/services/toast.service';
import { LogoComponent } from '../../../../../shared/components/logo';
import { SpinnerComponent } from '../../../../../shared/components/spinner';
import { ButtonDirective } from '../../../../../shared/components/button/button.directive';

@Component({
  selector: 'app-register-organization',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    LogoComponent,
    SpinnerComponent,
    ButtonDirective,
    TranslocoPipe,
  ],
  template: `
    <div class="min-h-screen flex flex-col bg-background text-foreground">
      <!-- Header -->
      <header class="py-6 px-4 sm:px-6 lg:px-8">
        <div class="max-w-2xl mx-auto">
          <app-logo size="lg" />
        </div>
      </header>

      <!-- Registration Form -->
      <main class="flex-1 flex items-center justify-center px-4 sm:px-6 lg:px-8 py-12">
        <div class="w-full max-w-2xl">
          <div class="bg-card border border-border rounded-lg shadow-sm p-8">
            <h1 class="text-3xl font-bold text-foreground mb-2">
              {{ 'register.title' | transloco }}
            </h1>
            <p class="text-muted-foreground mb-8">{{ 'register.subtitle' | transloco }}</p>

            <form [formGroup]="form" (ngSubmit)="onSubmit()">
              <!-- Organization Name -->
              <div class="mb-6">
                <label
                  for="organizationName"
                  class="block text-sm font-medium text-foreground mb-2"
                >
                  {{ 'register.organizationName' | transloco }}
                </label>
                <div class="relative">
                  <input
                    id="organizationName"
                    type="text"
                    formControlName="organizationName"
                    class="w-full px-4 py-2 border border-input rounded-md bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                    [class.border-destructive]="
                      form.get('organizationName')?.invalid && form.get('organizationName')?.touched
                    "
                    [placeholder]="'register.organizationNamePlaceholder' | transloco"
                  />
                  @if (checkingName()) {
                    <div class="absolute right-3 top-1/2 -translate-y-1/2">
                      <app-spinner size="sm" />
                    </div>
                  }
                  @if (nameCheckResult() === 'available') {
                    <div class="absolute right-3 top-1/2 -translate-y-1/2 text-green-600">✓</div>
                  }
                  @if (nameCheckResult() === 'taken') {
                    <div class="absolute right-3 top-1/2 -translate-y-1/2 text-destructive">✗</div>
                  }
                </div>
                @if (
                  form.get('organizationName')?.hasError('required') &&
                  form.get('organizationName')?.touched
                ) {
                  <p class="mt-1 text-sm text-destructive">
                    {{ 'register.organizationNameRequired' | transloco }}
                  </p>
                }
                @if (nameCheckResult() === 'taken') {
                  <p class="mt-1 text-sm text-destructive">
                    {{ 'register.organizationNameTaken' | transloco }}
                  </p>
                }
              </div>

              <!-- User Details Section -->
              <div class="mb-6">
                <h2 class="text-lg font-semibold text-foreground mb-4">
                  {{ 'register.yourDetails' | transloco }}
                </h2>

                <!-- Email -->
                <div class="mb-4">
                  <label for="userEmail" class="block text-sm font-medium text-foreground mb-2">
                    {{ 'register.email' | transloco }}
                  </label>
                  <input
                    id="userEmail"
                    type="email"
                    formControlName="userEmail"
                    class="w-full px-4 py-2 border border-input rounded-md bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                    [class.border-destructive]="
                      form.get('userEmail')?.invalid && form.get('userEmail')?.touched
                    "
                    [placeholder]="'register.emailPlaceholder' | transloco"
                  />
                  @if (
                    form.get('userEmail')?.hasError('required') && form.get('userEmail')?.touched
                  ) {
                    <p class="mt-1 text-sm text-destructive">{{ 'register.emailRequired' | transloco }}</p>
                  }
                  @if (form.get('userEmail')?.hasError('email') && form.get('userEmail')?.touched) {
                    <p class="mt-1 text-sm text-destructive">{{ 'register.emailInvalid' | transloco }}</p>
                  }
                </div>

                <!-- First Name & Last Name -->
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-4">
                  <div>
                    <label
                      for="userFirstName"
                      class="block text-sm font-medium text-foreground mb-2"
                    >
                      {{ 'register.firstName' | transloco }}
                    </label>
                    <input
                      id="userFirstName"
                      type="text"
                      formControlName="userFirstName"
                      class="w-full px-4 py-2 border border-input rounded-md bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                      [class.border-destructive]="
                        form.get('userFirstName')?.invalid && form.get('userFirstName')?.touched
                      "
                      [placeholder]="'register.firstNamePlaceholder' | transloco"
                    />
                    @if (
                      form.get('userFirstName')?.hasError('required') &&
                      form.get('userFirstName')?.touched
                    ) {
                      <p class="mt-1 text-sm text-destructive">
                        {{ 'register.firstNameRequired' | transloco }}
                      </p>
                    }
                  </div>

                  <div>
                    <label
                      for="userLastName"
                      class="block text-sm font-medium text-foreground mb-2"
                    >
                      {{ 'register.lastName' | transloco }}
                    </label>
                    <input
                      id="userLastName"
                      type="text"
                      formControlName="userLastName"
                      class="w-full px-4 py-2 border border-input rounded-md bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                      [class.border-destructive]="
                        form.get('userLastName')?.invalid && form.get('userLastName')?.touched
                      "
                      [placeholder]="'register.lastNamePlaceholder' | transloco"
                    />
                    @if (
                      form.get('userLastName')?.hasError('required') &&
                      form.get('userLastName')?.touched
                    ) {
                      <p class="mt-1 text-sm text-destructive">
                        {{ 'register.lastNameRequired' | transloco }}
                      </p>
                    }
                  </div>
                </div>

                <!-- Password -->
                <div class="mb-4">
                  <label for="userPassword" class="block text-sm font-medium text-foreground mb-2">
                    {{ 'register.password' | transloco }}
                  </label>
                  <input
                    id="userPassword"
                    type="password"
                    formControlName="userPassword"
                    class="w-full px-4 py-2 border border-input rounded-md bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                    [class.border-destructive]="
                      form.get('userPassword')?.invalid && form.get('userPassword')?.touched
                    "
                    placeholder="••••••••"
                  />
                  @if (
                    form.get('userPassword')?.hasError('required') &&
                    form.get('userPassword')?.touched
                  ) {
                    <p class="mt-1 text-sm text-destructive">
                      {{ 'register.passwordRequired' | transloco }}
                    </p>
                  }
                  @if (
                    form.get('userPassword')?.hasError('passwordRequirements') &&
                    form.get('userPassword')?.touched
                  ) {
                    <p class="mt-1 text-sm text-destructive">
                      {{ 'register.passwordRequirements' | transloco }}
                    </p>
                  }
                </div>

                <!-- Confirm Password -->
                <div>
                  <label
                    for="confirmPassword"
                    class="block text-sm font-medium text-foreground mb-2"
                  >
                    {{ 'register.confirmPassword' | transloco }}
                  </label>
                  <input
                    id="confirmPassword"
                    type="password"
                    formControlName="confirmPassword"
                    class="w-full px-4 py-2 border border-input rounded-md bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                    [class.border-destructive]="
                      form.get('confirmPassword')?.invalid && form.get('confirmPassword')?.touched
                    "
                    placeholder="••••••••"
                  />
                  @if (
                    form.get('confirmPassword')?.hasError('required') &&
                    form.get('confirmPassword')?.touched
                  ) {
                    <p class="mt-1 text-sm text-destructive">
                      {{ 'register.confirmPasswordRequired' | transloco }}
                    </p>
                  }
                  @if (
                    form.get('confirmPassword')?.hasError('passwordMismatch') &&
                    form.get('confirmPassword')?.dirty
                  ) {
                    <p class="mt-1 text-sm text-destructive">
                      {{ 'register.passwordMismatch' | transloco }}
                    </p>
                  }
                </div>
              </div>

              <!-- Submit Button -->
              <button
                appBtn
                type="submit"
                class="w-full"
                [disabled]="form.invalid || submitting() || nameCheckResult() === 'taken'"
              >
                @if (submitting()) {
                  <app-spinner size="sm" />
                  <span class="ml-2">{{ 'register.submitting' | transloco }}</span>
                } @else {
                  {{ 'register.submit' | transloco }}
                }
              </button>

              <!-- Sign In Link -->
              <div class="mt-6 text-center">
                <p class="text-sm text-muted-foreground">
                  {{ 'register.alreadyHaveAccount' | transloco }}
                  <button type="button" appBtn variant="link" class="p-0 h-auto" (click)="signIn()">
                    {{ 'register.signIn' | transloco }}
                  </button>
                </p>
              </div>
            </form>
          </div>
        </div>
      </main>
    </div>
  `,
})
export class RegisterOrganizationPage implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private sessionService = inject(SessionService);
  private organizationService = inject(OrganizationService);
  private toastService = inject(ToastService);
  private translocoService = inject(TranslocoService);
  private destroy$ = new Subject<void>();

  protected form!: FormGroup;
  protected submitting = signal(false);
  protected checkingName = signal(false);
  protected nameCheckResult = signal<'available' | 'taken' | null>(null);

  ngOnInit(): void {
    if (this.sessionService.isAuthenticated()) {
      this.router.navigate(['dashboard']);
      return;
    }

    this.form = this.fb.group({
      organizationName: ['', [Validators.required, Validators.minLength(2)]],
      userEmail: ['', [Validators.required, Validators.email]],
      userFirstName: ['', [Validators.required]],
      userLastName: ['', [Validators.required]],
      userPassword: ['', [Validators.required, this.passwordValidator]],
      confirmPassword: ['', [Validators.required]],
    });

    this.form.get('confirmPassword')?.addValidators(this.passwordMatchValidator.bind(this));

    this.setupNameCheck();
    this.setupPasswordConfirmation();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupNameCheck(): void {
    this.form
      .get('organizationName')
      ?.valueChanges.pipe(debounceTime(500), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe((name) => {
        if (name && name.length >= 2) {
          this.checkingName.set(true);
          this.nameCheckResult.set(null);

          this.organizationService
            .checkName(name)
            .pipe(
              catchError(() => {
                this.checkingName.set(false);
                return of({ nameAvailable: false });
              }),
              takeUntil(this.destroy$),
            )
            .subscribe((response) => {
              this.checkingName.set(false);
              this.nameCheckResult.set(response.nameAvailable ? 'available' : 'taken');
            });
        } else {
          this.nameCheckResult.set(null);
        }
      });
  }

  private setupPasswordConfirmation(): void {
    this.form
      .get('userPassword')
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        const confirmPassword = this.form.get('confirmPassword');
        if (confirmPassword?.dirty) {
          confirmPassword.updateValueAndValidity();
        }
      });

    this.form
      .get('confirmPassword')
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.form.get('confirmPassword')?.updateValueAndValidity({ emitEvent: false });
      });
  }

  private passwordValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;

    if (!value) {
      return null;
    }

    const hasLowercase = /[a-z]/.test(value);
    const hasUppercase = /[A-Z]/.test(value);
    const hasNumber = /[0-9]/.test(value);
    const hasSpecial = /[^A-Za-z0-9]/.test(value);
    const validLength = value.length >= 8 && value.length <= 100;

    const valid = hasLowercase && hasUppercase && hasNumber && hasSpecial && validLength;

    return valid ? null : { passwordRequirements: true };
  }

  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = this.form?.get('userPassword')?.value;
    const confirmPassword = control.value;

    if (!confirmPassword) {
      return null;
    }

    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.submitting() || this.nameCheckResult() === 'taken') {
      return;
    }

    this.submitting.set(true);

    const request = {
      organizationName: this.form.value.organizationName,
      userEmail: this.form.value.userEmail,
      userFirstName: this.form.value.userFirstName,
      userLastName: this.form.value.userLastName,
      userPassword: this.form.value.userPassword,
    };

    this.organizationService
      .register(request)
      .pipe(
        catchError((error) => {
          this.submitting.set(false);
          this.toastService.error(this.translocoService.translate('register.error.createFailed'));
          throw error;
        }),
        takeUntil(this.destroy$),
      )
      .subscribe(() => {
        this.toastService.success(this.translocoService.translate('register.success.created'));
        this.sessionService.login();
      });
  }

  protected signIn(): void {
    this.sessionService.login();
  }
}
