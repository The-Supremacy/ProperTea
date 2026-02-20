import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, map, of } from 'rxjs';
import { TranslocoPipe } from '@jsverse/transloco';
import { UnitService } from '../services/unit.service';
import { PropertyService } from '../../properties/services/property.service';
import { BuildingService } from '../../buildings/services/building.service';
import { ToastService } from '../../../core/services/toast.service';
import { UNIT_CATEGORIES } from '../models/unit.models';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmFormFieldImports } from '@spartan-ng/helm/form-field';
import { HlmLabel } from '@spartan-ng/helm/label';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { BrnSelectImports } from '@spartan-ng/brain/select';
import { HlmSelectImports } from '@spartan-ng/helm/select';
import { AutocompleteComponent } from '../../../../shared/components/autocomplete';
import { AddressFormComponent } from '../../../../shared/components/address-form';
import { UppercaseInputDirective } from '../../../../shared/directives';
import { HlmSheetImports } from '@spartan-ng/helm/sheet';

@Component({
  selector: 'app-create-unit-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    TranslocoPipe,
    HlmInput,
    HlmFormFieldImports,
    HlmLabel,
    HlmButton,
    HlmSpinner,
    BrnSelectImports,
    HlmSelectImports,
    AutocompleteComponent,
    AddressFormComponent,
    UppercaseInputDirective,
    HlmSheetImports,
  ],
  template: `
    <hlm-sheet side="right" [state]="open() ? 'open' : 'closed'" (closed)="onSheetClosed()">
      <hlm-sheet-content *hlmSheetPortal class="sm:max-w-md flex flex-col">
        <hlm-sheet-header>
          <h3 hlmSheetTitle>{{ 'units.newUnit' | transloco }}</h3>
          <p hlmSheetDescription>{{ 'units.createHint' | transloco }}</p>
        </hlm-sheet-header>

        <form [formGroup]="form" (ngSubmit)="submit()" class="flex min-h-0 flex-1 flex-col">
          <div class="flex-1 space-y-4 overflow-y-auto px-4">

            <!-- Property -->
            <div class="space-y-1.5">
              <label class="text-sm font-medium">{{ 'units.property' | transloco }} <span class="text-destructive">*</span></label>
              <app-autocomplete
                [value]="selectedPropertyId()"
                [placeholder]="'common.search'"
                [optionsProvider]="propertyOptionsProvider"
                (valueChange)="onPropertyChange($event)" />
            </div>

            <!-- Category + Floor (row) -->
            <div class="grid grid-cols-3 gap-3">
              <div class="col-span-2 space-y-1.5">
                <label hlmLabel>{{ 'units.category' | transloco }} <span class="text-destructive">*</span></label>
                <brn-select formControlName="category" class="block w-full">
                  <hlm-select-trigger class="w-full h-9">
                    <hlm-select-value />
                  </hlm-select-trigger>
                  <hlm-select-content>
                    @for (cat of categories; track cat) {
                      <hlm-option [value]="cat">{{ 'units.categories.' + cat.toLowerCase() | transloco }}</hlm-option>
                    }
                  </hlm-select-content>
                </brn-select>
              </div>
              <hlm-form-field>
                <label hlmLabel>{{ 'units.floor' | transloco }}</label>
                <input hlmInput type="number" formControlName="floor" class="w-full" />
              </hlm-form-field>
            </div>

            <!-- Building (not shown for House) -->
            @if (showBuilding()) {
              <div class="space-y-1.5">
                <label class="text-sm font-medium">
                  {{ 'units.building' | transloco }}
                  @if (buildingRequired()) { <span class="text-destructive">*</span> }
                </label>
                <app-autocomplete
                  [value]="selectedBuildingId()"
                  [placeholder]="'common.search'"
                  [disabled]="!selectedPropertyId()"
                  [optionsProvider]="buildingOptionsProvider()"
                  (valueChange)="onBuildingChange($event)" />
                @if (form.controls.buildingId.touched && form.controls.buildingId.hasError('buildingRequired')) {
                  <p class="text-destructive text-sm">{{ 'units.buildingRequired' | transloco }}</p>
                }
              </div>
            }

            <!-- Code -->
            <hlm-form-field>
              <label hlmLabel>{{ 'units.code' | transloco }} <span class="text-destructive">*</span></label>
              <input hlmInput appUppercase formControlName="code" [placeholder]="'units.codePlaceholder' | transloco" class="w-full" />
              @if (form.controls.code.touched && form.controls.code.hasError('required')) {
                <hlm-error>{{ 'units.codeRequired' | transloco }}</hlm-error>
              }
              @if (form.controls.code.touched && form.controls.code.hasError('maxlength')) {
                <hlm-error>{{ 'units.codeTooLong' | transloco }}</hlm-error>
              }
              @if (form.controls.code.touched && form.controls.code.hasError('pattern')) {
                <hlm-error>{{ 'common.codeInvalidFormat' | transloco }}</hlm-error>
              }
            </hlm-form-field>

            <!-- Address -->
            <app-address-form [addressGroup]="$any(form.controls.address)" [required]="true" />
          </div>

          <hlm-sheet-footer>
            <button type="button" hlmBtn variant="outline" class="flex-1" hlmSheetClose [disabled]="isSubmitting()">{{ 'common.cancel' | transloco }}</button>
            <button type="submit" hlmBtn class="flex-1" [disabled]="!canSubmit()">
              @if (isSubmitting()) { <hlm-spinner size="sm" /> }
              {{ 'common.create' | transloco }}
            </button>
          </hlm-sheet-footer>
        </form>
      </hlm-sheet-content>
    </hlm-sheet>
  `,
})
export class CreateUnitDrawerComponent {
  private fb = inject(FormBuilder);
  private unitService = inject(UnitService);
  private propertyService = inject(PropertyService);
  private buildingService = inject(BuildingService);
  private router = inject(Router);
  private toastService = inject(ToastService);

  open = input.required<boolean>();
  defaultPropertyId = input<string | null>(null);
  navigateAfterCreate = input<boolean>(true);

  openChange = output<boolean>();
  created = output<string>();

  protected isSubmitting = signal(false);
  protected readonly categories = UNIT_CATEGORIES;

  // Cascading selection state — signals drive both the form controls and autocomplete providers.
  protected readonly selectedPropertyId = signal('');
  protected readonly selectedBuildingId = signal('');

  apartmentBuildingValidator(control: AbstractControl): ValidationErrors | null {
    const category = control.parent?.get('category')?.value as string | undefined;
    if (category === 'Apartment' && !control.value) {
      return { buildingRequired: true };
    }
    return null;
  }

  protected form = this.fb.nonNullable.group({
    propertyId: ['', [Validators.required]],
    buildingId: ['', [this.apartmentBuildingValidator.bind(this)]],
    category: ['Apartment', [Validators.required]],
    code: ['', [Validators.required, Validators.maxLength(10), Validators.pattern(/^[A-Z0-9]*$/)]],
    floor: [null as number | null],
    address: this.fb.group({
      country: [''],
      streetAddress: ['', [Validators.required]],
      city: ['', [Validators.required]],
      zipCode: ['', [Validators.required]],
    }),
  });

  constructor() {
    const destroyRef = inject(DestroyRef);

    effect(() => {
      const defaultId = this.defaultPropertyId();
      if (defaultId && this.open()) {
        this.selectedPropertyId.set(defaultId);
        this.form.controls.propertyId.setValue(defaultId);
      }
    });
    this.form.controls.category.valueChanges
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe(() => this.form.controls.buildingId.updateValueAndValidity({ emitEvent: false }));
  }

  private formStatus = toSignal(this.form.statusChanges, { initialValue: this.form.status });
  private categoryValue = toSignal(this.form.controls.category.valueChanges, {
    initialValue: this.form.controls.category.value,
  });

  protected canSubmit = computed(() => this.formStatus() === 'VALID' && !this.isSubmitting());
  protected showBuilding = computed(() => this.categoryValue() !== 'House');
  protected buildingRequired = computed(() => this.categoryValue() === 'Apartment');

  // Providers as computed signals — a new function reference is emitted on each
  // dependency change, which causes AutocompleteComponent's resource to reload.
  protected readonly propertyOptionsProvider = () =>
    this.propertyService
      .select()
      .pipe(map((items) => items.map((p) => ({ value: p.id, label: `${p.code} – ${p.name}` }))));

  protected readonly buildingOptionsProvider = computed(() => {
    const propertyId = this.selectedPropertyId();
    return () =>
      propertyId
        ? this.buildingService.select(propertyId).pipe(map((items) => items.map((b) => ({ value: b.id, label: `${b.code} – ${b.name}` }))))
        : of([]);
  });

  protected onPropertyChange(value: string): void {
    this.selectedPropertyId.set(value);
    this.selectedBuildingId.set('');
    this.form.controls.propertyId.setValue(value);
    this.form.controls.propertyId.markAsTouched();
    this.form.controls.buildingId.setValue('');

    // Pre-fill address from parent property
    if (value) {
      this.propertyService.get(value).subscribe((property) => {
        if (property?.address) {
          this.form.controls.address.patchValue({
            country: property.address.country ?? '',
            streetAddress: property.address.streetAddress ?? '',
            city: property.address.city ?? '',
            zipCode: property.address.zipCode ?? '',
          });
        }
      });
    }
  }

  protected onBuildingChange(value: string): void {
    this.selectedBuildingId.set(value);
    this.form.controls.buildingId.setValue(value);

    // Pre-fill address from parent building (overrides property address)
    if (value) {
      this.buildingService.get(value).subscribe((building) => {
        if (building?.address) {
          this.form.controls.address.patchValue({
            country: building.address.country ?? '',
            streetAddress: building.address.streetAddress ?? '',
            city: building.address.city ?? '',
            zipCode: building.address.zipCode ?? '',
          });
        }
      });
    }
  }

  close(): void {
    if (this.isSubmitting()) return;
    this.openChange.emit(false);
    this.selectedPropertyId.set('');
    this.selectedBuildingId.set('');
    this.form.reset({ category: 'Apartment' });
  }

  onSheetClosed(): void {
    this.selectedPropertyId.set('');
    this.selectedBuildingId.set('');
    this.form.reset({ category: 'Apartment' });
    this.openChange.emit(false);
  }

  submit(): void {
    if (this.isSubmitting()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.isSubmitting.set(true);
    const v = this.form.getRawValue();
    const isHouse = v.category === 'House';

    this.unitService
      .create({
        propertyId: v.propertyId,
        buildingId: !isHouse && v.buildingId ? v.buildingId : undefined,
        code: v.code.trim(),
        category: v.category as never,
        floor: v.floor ?? undefined,
        address: {
          country: v.address.country || 'UA',
          streetAddress: v.address.streetAddress ?? '',
          city: v.address.city ?? '',
          zipCode: v.address.zipCode ?? '',
        },
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: (response) => {
          this.toastService.success('units.success.created');
          this.created.emit(response.id);
          this.isSubmitting.set(false);
          this.close();
          if (this.navigateAfterCreate()) {
            this.router.navigate(['/units', response.id]);
          }
        },
        error: () => {
          // The global error interceptor already shows a toast with the API error detail.
        },
      });
  }
}
