import { Component, inject, signal, input, output, computed, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, firstValueFrom, map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoPipe } from '@jsverse/transloco';
import { PropertyService } from '../services/property.service';
import { CompanyService } from '../../companies/services/company.service';
import { ToastService } from '../../../core/services/toast.service';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmFormFieldImports } from '@spartan-ng/helm/form-field';
import { HlmLabel } from '@spartan-ng/helm/label';
import { HlmButton } from '@spartan-ng/helm/button';
import { IconComponent } from '../../../../shared/components/icon';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { AutocompleteComponent, AutocompleteOption } from '../../../../shared/components/autocomplete';
import { HlmSheetImports } from '@spartan-ng/helm/sheet';

@Component({
  selector: 'app-create-property-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    TranslocoPipe,
    HlmInput,
    HlmFormFieldImports,
    HlmLabel,
    HlmButton,
    IconComponent,
    HlmSpinner,
    AutocompleteComponent,
    HlmSheetImports,
  ],
  templateUrl: './create-property-drawer.component.html',
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

  onSheetClosed(): void {
    this.form.reset();
    this.openChange.emit(false);
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
