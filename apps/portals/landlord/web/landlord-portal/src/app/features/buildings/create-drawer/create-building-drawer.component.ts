import { Component, inject, signal, input, output, computed, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoPipe } from '@jsverse/transloco';
import { BuildingService } from '../services/building.service';
import { PropertyService } from '../../properties/services/property.service';
import { ToastService } from '../../../core/services/toast.service';
import { TextInputDirective } from '../../../../shared/components/form-field/text-input.directive';
import { ValidationErrorComponent } from '../../../../shared/components/form-field/validation-error.component';
import { ButtonDirective } from '../../../../shared/components/button';
import { IconComponent } from '../../../../shared/components/icon';
import { SpinnerComponent } from '../../../../shared/components/spinner';
import { AutocompleteComponent } from '../../../../shared/components/autocomplete';
import { DrawerFooterDirective } from '../../../../shared/components/drawer-footer';

@Component({
  selector: 'app-create-building-drawer',
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
    DrawerFooterDirective,
  ],
  templateUrl: './create-building-drawer.component.html',
  styleUrl: './create-building-drawer.component.css',
})
export class CreateBuildingDrawerComponent {
  private fb = inject(FormBuilder);
  private buildingService = inject(BuildingService);
  private propertyService = inject(PropertyService);
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
    propertyId: ['', [Validators.required]],
    code: ['', [Validators.required, Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
  });

  // Convert form status to signal for reactivity
  private formStatus = toSignal(this.form.statusChanges, { initialValue: this.form.status });

  // Computed values
  protected canSubmit = computed(() => this.formStatus() === 'VALID' && !this.isSubmitting());

  protected getPropertyOptionsProvider = () => {
    return this.propertyService
      .select()
      .pipe(
        map((properties) =>
          properties.map((p) => ({ value: p.id, label: p.name }))
        )
      );
  };

  protected onPropertyChange(value: string): void {
    this.form.controls.propertyId.setValue(value);
    this.form.controls.propertyId.markAsTouched();
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

    this.buildingService
      .create(formValue.propertyId, {
        code: formValue.code.trim(),
        name: formValue.name.trim(),
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: (response) => {
          this.toastService.success('buildings.success.created');
          this.close();
          this.router.navigate(['/buildings', response.id]);
        },
        error: () => {
          this.toastService.error('buildings.error.createFailed');
        },
      });
  }
}
