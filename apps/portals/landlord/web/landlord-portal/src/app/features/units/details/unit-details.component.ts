import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom, map, of } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { HlmTabsImports } from '@spartan-ng/helm/tabs';
import { HlmInput } from '@spartan-ng/helm/input';
import { EntityDetailsConfig, EntityDetailsViewComponent } from '../../../../shared/components/entity-details-view';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmFormFieldImports } from '@spartan-ng/helm/form-field';
import { HlmLabel } from '@spartan-ng/helm/label';
import { BrnSelectImports } from '@spartan-ng/brain/select';
import { HlmSelectImports } from '@spartan-ng/helm/select';
import { StatusBadgeDirective } from '../../../../shared/directives';
import { UnitService } from '../services/unit.service';
import { UnitCategory, UnitDetailResponse, UNIT_CATEGORIES } from '../models/unit.models';
import { BuildingService } from '../../buildings/services/building.service';
import { PropertyService } from '../../properties/services/property.service';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { UppercaseInputDirective } from '../../../../shared/directives';
import { AutocompleteComponent } from '../../../../shared/components/autocomplete';
import { UnitAuditLogComponent } from '../audit-log/unit-audit-log.component';

@Component({
  selector: 'app-unit-details',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    TranslocoPipe,
    EntityDetailsViewComponent,
    HlmTabsImports,
    HlmInput,
    HlmSpinner,
    HlmCardImports,
    HlmFormFieldImports,
    HlmLabel,
    BrnSelectImports,
    HlmSelectImports,
    StatusBadgeDirective,
    UppercaseInputDirective,
    AutocompleteComponent,
    UnitAuditLogComponent,
  ],
  template: `
    <app-entity-details-view [config]="detailsConfig()" [loading]="loading()" (refresh)="loadUnit(unitId())">
      @if (loading() && !unit()) {
        <div class="flex items-center justify-center py-12">
          <hlm-spinner size="lg" />
        </div>
      }

      @if (unit()) {
        <hlm-tabs [tab]="selectedTab()" (tabActivated)="selectedTab.set($event)">
          <div class="border-b px-4">
            <hlm-tabs-list class="justify-start gap-4 rounded-none bg-transparent">
              <button hlmTabsTrigger="details">{{ 'common.details' | transloco }}</button>
              <button hlmTabsTrigger="history">{{ 'common.history' | transloco }}</button>
            </hlm-tabs-list>
          </div>

          <div hlmTabsContent="details">
            <div class="py-6">
              <div class="container mx-auto px-4">
                <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
                  @if (form) {
                    <form [formGroup]="form" (ngSubmit)="save()" class="contents">

                      <!-- Basic Info Card -->
                      <section hlmCard class="lg:col-span-2 order-1">
                        <div hlmCardHeader>
                          <h3 hlmCardTitle>{{ 'units.basicInfo' | transloco }}</h3>
                        </div>
                        <div hlmCardContent>
                          <div class="grid grid-cols-12 gap-4">
                            <div class="col-span-12 sm:col-span-6 lg:col-span-3 space-y-1.5">
                              <label hlmLabel>{{ 'units.unitReference' | transloco }}</label>
                              <div class="flex h-10 w-full items-center rounded-md border border-input bg-muted px-3 text-sm text-muted-foreground">
                                {{ unit()?.unitReference }}
                              </div>
                            </div>

                            <div class="col-span-12 sm:col-span-6 lg:col-span-3 space-y-1.5">
                              <label hlmLabel>{{ 'units.category' | transloco }}</label>
                              <brn-select formControlName="category" class="block w-full">
                                <hlm-select-trigger class="w-full">
                                  <hlm-select-value />
                                </hlm-select-trigger>
                                <hlm-select-content>
                                  @for (cat of categories; track cat) {
                                    <hlm-option [value]="cat">{{ 'units.categories.' + cat.toLowerCase() | transloco }}</hlm-option>
                                  }
                                </hlm-select-content>
                              </brn-select>
                            </div>

                            <div class="col-span-12 sm:col-span-6 lg:col-span-3">
                              <hlm-form-field class="w-full">
                                <label hlmLabel for="unit-code">{{ 'units.code' | transloco }}</label>
                                <input id="unit-code" hlmInput appUppercase formControlName="code"
                                  [placeholder]="'units.codePlaceholder' | transloco" class="w-full" />
                                @if (form.controls['code'].touched && form.controls['code'].hasError('required')) {
                                  <hlm-error>{{ 'units.codeRequired' | transloco }}</hlm-error>
                                }
                                @if (form.controls['code'].touched && form.controls['code'].hasError('maxlength')) {
                                  <hlm-error>{{ 'units.codeTooLong' | transloco }}</hlm-error>
                                }
                                @if (form.controls['code'].touched && form.controls['code'].hasError('pattern')) {
                                  <hlm-error>{{ 'common.codeInvalidFormat' | transloco }}</hlm-error>
                                }
                              </hlm-form-field>
                            </div>

                            <div class="col-span-12 sm:col-span-6 lg:col-span-3">
                              <hlm-form-field class="w-full">
                                <label hlmLabel for="unit-floor">{{ 'units.floor' | transloco }}</label>
                                <input id="unit-floor" hlmInput type="number" formControlName="floor" class="w-full" />
                              </hlm-form-field>
                            </div>
                          </div>
                        </div>
                      </section>

                      <!-- Sidebar -->
                      <div class="order-2 space-y-6">
                        <!-- Status Card -->
                        <section hlmCard>
                          <div hlmCardHeader>
                            <h3 hlmCardTitle>{{ 'common.status' | transloco }}</h3>
                          </div>
                          <div hlmCardContent>
                            <div class="grid grid-cols-2 gap-4">
                              <div>
                                <p class="text-sm text-muted-foreground mb-1">{{ 'common.currentStatus' | transloco }}</p>
                                <span [appStatusBadge]="unit()?.status || 'Active'">
                                  {{ 'common.statuses.' + (unit()?.status || 'Active') | transloco }}
                                </span>
                              </div>
                              <div>
                                <p class="text-sm text-muted-foreground mb-1">{{ 'common.createdAt' | transloco }}</p>
                                <p class="text-sm">{{ unit()?.createdAt | date: 'medium' }}</p>
                              </div>
                            </div>
                          </div>
                        </section>

                        <!-- Quick Actions Card -->
                        <section hlmCard class="hidden lg:block">
                          <div hlmCardHeader>
                            <h3 hlmCardTitle>{{ 'common.quickActions' | transloco }}</h3>
                          </div>
                          <div hlmCardContent>
                            <p class="py-4 text-center text-sm text-muted-foreground">
                              {{ 'common.noQuickActions' | transloco }}
                            </p>
                          </div>
                        </section>
                      </div>

                      <!-- Location Card -->
                      <section hlmCard class="lg:col-span-2 order-3">
                        <div hlmCardHeader>
                          <h3 hlmCardTitle>{{ 'units.location' | transloco }}</h3>
                        </div>
                        <div hlmCardContent>
                          <div class="grid grid-cols-12 gap-4">
                            <div class="col-span-12 sm:col-span-4 space-y-1.5">
                              <label hlmLabel>{{ 'units.property' | transloco }}</label>
                              <app-autocomplete
                                [value]="selectedPropertyId()"
                                [placeholder]="'common.search'"
                                [optionsProvider]="propertyOptionsProvider"
                                (valueChange)="onPropertyChange($event)" />
                            </div>

                            <div class="col-span-12 sm:col-span-4 space-y-1.5">
                              <label hlmLabel>{{ 'units.building' | transloco }}</label>
                              <app-autocomplete
                                [value]="selectedBuildingId()"
                                [placeholder]="'common.search'"
                                [optionsProvider]="buildingOptionsProvider()"
                                (valueChange)="onBuildingChange($event)" />
                            </div>

                            <div class="col-span-12 sm:col-span-4 space-y-1.5">
                              <label hlmLabel>{{ 'units.entrance' | transloco }}</label>
                              <app-autocomplete
                                [value]="selectedEntranceId()"
                                [placeholder]="'common.search'"
                                [disabled]="!selectedBuildingId()"
                                [optionsProvider]="entranceOptionsProvider()"
                                (valueChange)="onEntranceChange($event)" />
                            </div>
                          </div>
                        </div>
                      </section>

                      <!-- Address Card -->
                      <section hlmCard class="lg:col-span-2 order-4">
                        <div hlmCardHeader>
                          <h3 hlmCardTitle>{{ 'units.address' | transloco }}</h3>
                        </div>
                        <div hlmCardContent>
                          <div formGroupName="address" class="grid grid-cols-12 gap-4">
                            <div class="col-span-12 lg:col-span-6">
                              <hlm-form-field class="w-full">
                                <label hlmLabel for="unit-street">{{ 'address.streetAddress' | transloco }}</label>
                                <input id="unit-street" hlmInput formControlName="streetAddress" class="w-full" />
                                @if (form.get('address.streetAddress')?.touched && form.get('address.streetAddress')?.hasError('required')) {
                                  <hlm-error>{{ 'address.streetAddressRequired' | transloco }}</hlm-error>
                                }
                              </hlm-form-field>
                            </div>

                            <div class="col-span-12 sm:col-span-4 lg:col-span-2">
                              <hlm-form-field class="w-full">
                                <label hlmLabel for="unit-country">{{ 'address.country' | transloco }}</label>
                                <input id="unit-country" hlmInput formControlName="country" class="w-full" />
                                @if (form.get('address.country')?.touched && form.get('address.country')?.hasError('required')) {
                                  <hlm-error>{{ 'address.countryRequired' | transloco }}</hlm-error>
                                }
                              </hlm-form-field>
                            </div>

                            <div class="col-span-12 sm:col-span-4 lg:col-span-2">
                              <hlm-form-field class="w-full">
                                <label hlmLabel for="unit-city">{{ 'address.city' | transloco }}</label>
                                <input id="unit-city" hlmInput formControlName="city" class="w-full" />
                                @if (form.get('address.city')?.touched && form.get('address.city')?.hasError('required')) {
                                  <hlm-error>{{ 'address.cityRequired' | transloco }}</hlm-error>
                                }
                              </hlm-form-field>
                            </div>

                            <div class="col-span-12 sm:col-span-4 lg:col-span-2">
                              <hlm-form-field class="w-full">
                                <label hlmLabel for="unit-zip">{{ 'address.zipCode' | transloco }}</label>
                                <input id="unit-zip" hlmInput formControlName="zipCode" class="w-full" />
                                @if (form.get('address.zipCode')?.touched && form.get('address.zipCode')?.hasError('required')) {
                                  <hlm-error>{{ 'address.zipCodeRequired' | transloco }}</hlm-error>
                                }
                              </hlm-form-field>
                            </div>
                          </div>
                        </div>
                      </section>

                    </form>
                  }
                </div>
              </div>
            </div>
          </div>

          <div hlmTabsContent="history">
            <ng-template hlmTabsContentLazy>
              <div class="py-6">
                <div class="container mx-auto px-4 max-w-4xl">
                  <app-unit-audit-log [unitId]="unitId()" />
                </div>
              </div>
            </ng-template>
          </div>

        </hlm-tabs>
      }
    </app-entity-details-view>
  `,
})
export class UnitDetailsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private unitService = inject(UnitService);
  private propertyService = inject(PropertyService);
  private buildingService = inject(BuildingService);
  private dialogService = inject(DialogService);
  private toastService = inject(ToastService);
  private translocoService = inject(TranslocoService);

  protected readonly categories = UNIT_CATEGORIES;

  unit = signal<UnitDetailResponse | null>(null);
  saving = signal(false);
  loading = signal(false);
  unitId = signal<string>('');
  selectedTab = signal<string>('details');
  protected readonly selectedPropertyId = signal('');
  protected readonly selectedBuildingId = signal('');
  protected readonly selectedEntranceId = signal('');

  protected readonly propertyOptionsProvider = () =>
    this.propertyService
      .select()
      .pipe(map((items) => items.map((p) => ({ value: p.id, label: `${p.code} \u2013 ${p.name}` }))));

  protected readonly buildingOptionsProvider = computed(() => {
    const propertyId = this.selectedPropertyId();
    return () =>
      propertyId
        ? this.buildingService
            .select(propertyId)
            .pipe(map((items) => items.map((b) => ({ value: b.id, label: `${b.code} \u2013 ${b.name}` }))))
        : of([]);
  });

  protected readonly entranceOptionsProvider = computed(() => {
    const buildingId = this.selectedBuildingId();
    return () =>
      buildingId
        ? this.buildingService
            .get(buildingId)
            .pipe(map((b) => b?.entrances.map((e) => ({ value: e.id, label: `${e.code} – ${e.name}` })) ?? []))
        : of([]);
  });

  detailsConfig = computed<EntityDetailsConfig>(() => ({
    title: this.translocoService.translate('units.details.breadcrumb'),
    subtitle: this.unit()?.unitReference,
    showBackButton: true,
    showRefresh: true,
    primaryActions: [
      {
        label: 'common.save',
        icon: 'save',
        variant: 'default',
        handler: () => this.save(),
        disabled: this.form?.invalid || this.saving(),
      },
    ],
    secondaryActions: [
      {
        label: 'common.delete',
        icon: 'delete',
        variant: 'destructive',
        handler: () => this.deleteUnit(),
        separatorBefore: true,
      },
    ],
  }));

  form!: FormGroup;

  ngOnInit(): void {
    this.route.params.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const id = params['id'] as string;
      this.unitId.set(id);
      this.loadUnit(id);
    });
  }

  protected loadUnit(id: string): void {
    this.loading.set(true);
    this.unitService
      .get(id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (unit) => {
          this.unit.set(unit);
          if (unit) {
            this.selectedPropertyId.set(unit.propertyId);
            this.selectedBuildingId.set(unit.buildingId ?? '');
            this.selectedEntranceId.set(unit.entranceId ?? '');

            this.form = this.fb.nonNullable.group({
              code: [unit.code, [Validators.required, Validators.maxLength(10), Validators.pattern(/^[A-Z0-9]*$/)]],
              category: [unit.category as UnitCategory, [Validators.required]],
              floor: [unit.floor ?? (null as number | null)],
              address: this.fb.group({
                country: [unit.address?.country ?? 'UA', [Validators.required]],
                streetAddress: [unit.address?.streetAddress ?? '', [Validators.required]],
                city: [unit.address?.city ?? '', [Validators.required]],
                zipCode: [unit.address?.zipCode ?? '', [Validators.required]],
              }),
            });
          }
        },
        error: () => {
          this.toastService.error('units.error.loadFailed');
        },
      });
  }

  protected onPropertyChange(value: string): void {
    this.selectedPropertyId.set(value);
    this.selectedBuildingId.set('');
    this.selectedEntranceId.set('');
  }

  protected onBuildingChange(value: string): void {
    this.selectedBuildingId.set(value);
    this.selectedEntranceId.set('');
  }

  protected onEntranceChange(value: string): void {
    this.selectedEntranceId.set(value);
  }

  save(): void {
    if (!this.form?.valid || this.saving()) return;

    this.saving.set(true);
    const v = this.form.getRawValue();
    const addr = v.address as { country: string; streetAddress: string; city: string; zipCode: string };

    this.unitService
      .update(this.unitId(), {
        propertyId: this.selectedPropertyId(),
        buildingId: this.selectedBuildingId() || undefined,
        entranceId: this.selectedEntranceId() || undefined,
        code: v.code.trim(),
        category: v.category as UnitCategory,
        address: {
          country: addr.country,
          streetAddress: addr.streetAddress,
          city: addr.city,
          zipCode: addr.zipCode,
        },
        floor: v.floor ?? undefined,
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.toastService.success('units.success.updated');
          this.loadUnit(this.unitId());
        },
        error: () => {
          // Global error interceptor already shows the API error toast.
        },
      });
  }

  async deleteUnit(): Promise<void> {
    const unit = this.unit();
    if (!unit) return;

    const confirmed = await firstValueFrom(
      this.dialogService.confirm({
        title: this.translocoService.translate('units.deleteConfirmTitle'),
        description: this.translocoService.translate('units.deleteConfirmMessage', { code: unit.code }),
        confirmText: this.translocoService.translate('common.delete'),
        variant: 'destructive',
      }),
    );

    if (!confirmed) return;

    this.unitService.delete(unit.id).subscribe({
      next: () => {
        this.toastService.success('units.success.deleted');
        this.router.navigate(['/units']);
      },
      error: () => {
        this.toastService.error('units.error.deleteFailed');
      },
    });
  }
}
