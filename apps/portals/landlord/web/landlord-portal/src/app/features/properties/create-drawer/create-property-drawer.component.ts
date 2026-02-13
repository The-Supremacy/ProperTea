import { Component, inject, signal, input, output, computed, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, firstValueFrom, map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoPipe } from '@jsverse/transloco';
import { PropertyService } from '../services/property.service';
import { CompanyService } from '../../companies/services/company.service';
import { ToastService } from '../../../core/services/toast.service';
import { TextInputDirective } from '../../../../shared/components/form-field/text-input.directive';
import { ValidationErrorComponent } from '../../../../shared/components/form-field/validation-error.component';
import { ButtonDirective } from '../../../../shared/components/button';
import { IconComponent } from '../../../../shared/components/icon';
import { SpinnerComponent } from '../../../../shared/components/spinner';
import { AutocompleteComponent, AutocompleteOption } from '../../../../shared/components/autocomplete';

@Component({
  selector: 'app-create-property-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    TranslocoPipe,
    TextInputDirective,
    ValidationErrorComponent,
    ButtonDirective,
    IconComponent,
    SpinnerComponent,
    AutocompleteComponent,
  ],
  templateUrl: './create-property-drawer.component.html',
  styleUrl: './create-property-drawer.component.css',
})
export class CreatePropertyDrawerComponent {
  private fb = inject(FormBuilder);
  private propertyService = inject(PropertyService);
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
    companyId: ['', [Validators.required]],
    code: ['', [Validators.required, Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    address: ['', [Validators.required, Validators.maxLength(500)]],
    squareFootage: [null as number | null],
  });

  // Convert form status to signal for reactivity
  private formStatus = toSignal(this.form.statusChanges, { initialValue: this.form.status });

  // Computed values
  protected canSubmit = computed(() => this.formStatus() === 'VALID' && !this.isSubmitting());

  protected getCompanyOptionsProvider = () => {
    return this.companyService
      .select()
      .pipe(
        map((companies) =>
          companies.map((c) => ({ value: c.id, label: c.name }))
        )
      );
  };

  protected onCompanyChange(value: string): void {
    this.form.controls.companyId.setValue(value);
    this.form.controls.companyId.markAsTouched();
  }

  close(): void {
    if (this.isSubmitting()) return;
    this.openChange.emit(false);
    this.form.reset();
  }

  submit(): void {
    if (!this.canSubmit()) return;

    this.isSubmitting.set(true);
    const formValue = this.form.getRawValue();

    this.propertyService
      .create({
        companyId: formValue.companyId,
        code: formValue.code.trim(),
        name: formValue.name.trim(),
        address: formValue.address.trim(),
        squareFootage: formValue.squareFootage ?? undefined,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: (response) => {
          this.toastService.success('properties.success.created');
          this.close();
          this.router.navigate(['/properties', response.id]);
        },
        error: () => {
          this.toastService.error('properties.error.createFailed');
        },
      });
  }
}
