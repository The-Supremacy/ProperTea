import { Component, inject, signal, input, output, computed, effect, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoPipe } from '@jsverse/transloco';
import { CompanyService } from '../services/company.service';
import { ToastService } from '../../../core/services/toast.service';
import { uniqueCompanyName } from '../validators/company-name.validators';
import { TextInputDirective } from '../../../../shared/components/form-field/text-input.directive';
import { ValidationErrorComponent } from '../../../../shared/components/form-field/validation-error.component';
import { ButtonDirective } from '../../../../shared/components/button';
import { IconComponent } from '../../../../shared/components/icon';
import { SpinnerComponent } from '../../../../shared/components/spinner';

@Component({
  selector: 'app-create-company-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    TranslocoPipe,
    TextInputDirective,
    ValidationErrorComponent,
    ButtonDirective,
    IconComponent,
    SpinnerComponent,
  ],
  templateUrl: './create-company-drawer.component.html',
  styleUrl: './create-company-drawer.component.css',
})
export class CreateCompanyDrawerComponent {
  private fb = inject(FormBuilder);
  private companyService = inject(CompanyService);
  private router = inject(Router);
  private toastService = inject(ToastService);

  // Inputs
  open = input.required<boolean>();

  // Outputs
  openChange = output<boolean>();

  // State
  protected isSubmitting = signal(false);

  // Form
  protected form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)], [uniqueCompanyName()]],
  });

  // Convert form status to signal for reactivity
  private formStatus = toSignal(this.form.statusChanges, { initialValue: this.form.status });

  // Computed values
  protected nameControl = computed(() => this.form.controls.name);
  protected canSubmit = computed(() => this.formStatus() === 'VALID' && !this.isSubmitting());

  close(): void {
    if (this.isSubmitting()) return;
    this.openChange.emit(false);
    this.form.reset();
  }

  submit(): void {
    if (!this.canSubmit()) return;

    this.isSubmitting.set(true);
    const request = { name: this.form.value.name!.trim() };

    this.companyService
      .create(request)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: (response) => {
          this.toastService.success('companies.success.created');
          this.close();
          this.router.navigate(['/companies', response.id]);
        },
        error: () => {
          this.toastService.error('companies.error.createFailed');
        },
      });
  }
}
