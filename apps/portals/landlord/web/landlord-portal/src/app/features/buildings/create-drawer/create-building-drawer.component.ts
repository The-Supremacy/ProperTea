import { Component, inject, signal, input, output, computed, effect, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoPipe } from '@jsverse/transloco';
import { BuildingService } from '../services/building.service';
import { PropertyService } from '../../properties/services/property.service';
import { ToastService } from '../../../core/services/toast.service';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmFormFieldImports } from '@spartan-ng/helm/form-field';
import { HlmLabel } from '@spartan-ng/helm/label';
import { HlmButton } from '@spartan-ng/helm/button';
import { IconComponent } from '../../../../shared/components/icon';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { AutocompleteComponent } from '../../../../shared/components/autocomplete';
import { HlmSheetImports } from '@spartan-ng/helm/sheet';

@Component({
  selector: 'app-create-building-drawer',
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
  templateUrl: './create-building-drawer.component.html',
})
export class CreateBuildingDrawerComponent {
  private fb = inject(FormBuilder);
  private buildingService = inject(BuildingService);
  private propertyService = inject(PropertyService);
  private router = inject(Router);
  private toastService = inject(ToastService);

  // Inputs
  open = input.required<boolean>();
  defaultPropertyId = input<string | null>(null);
  navigateAfterCreate = input<boolean>(true);

  // Outputs
  openChange = output<boolean>();
  created = output<string>();

  // State
  protected isSubmitting = signal(false);

  // Form
  protected form = this.fb.nonNullable.group({
    propertyId: ['', [Validators.required]],
    code: ['', [Validators.required, Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
  });

  constructor() {
    // Pre-fill property when the drawer opens with a default
    effect(() => {
      const defaultId = this.defaultPropertyId();
      if (defaultId && this.open()) {
        this.form.controls.propertyId.setValue(defaultId);
      }
    });
  }

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

  onSheetClosed(): void {
    this.form.reset();
    this.openChange.emit(false);
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
          this.created.emit(response.id);
          this.isSubmitting.set(false); // clear before close() so the guard doesn't block
          this.close();
          if (this.navigateAfterCreate()) {
            this.router.navigate(['/buildings', response.id]);
          }
        },
        error: () => {
          this.toastService.error('buildings.error.createFailed');
        },
      });
  }
}
