import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom, map, Subject, takeUntil } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Tabs, Tab, TabContent, TabList, TabPanel } from '@angular/aria/tabs';
import { EntityDetailsConfig, EntityDetailsViewComponent } from '../../../../shared/components/entity-details-view';
import { SpinnerComponent } from '../../../../shared/components/spinner';
import { StatusBadgeDirective } from '../../../../shared/directives';
import { BuildingService } from '../services/building.service';
import { PropertyService } from '../../properties/services/property.service';
import { BuildingDetailResponse, UpdateBuildingRequest } from '../models/building.models';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { BuildingAuditLogComponent } from '../audit-log/building-audit-log.component';

@Component({
  selector: 'app-building-details',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    TranslocoPipe,
    EntityDetailsViewComponent,
    Tabs,
    TabList,
    Tab,
    TabPanel,
    TabContent,
    SpinnerComponent,
    StatusBadgeDirective,
    BuildingAuditLogComponent,
  ],
  templateUrl: './building-details.component.html',
  styleUrl: './building-details.component.css',
})
export class BuildingDetailsComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private buildingService = inject(BuildingService);
  private propertyService = inject(PropertyService);
  private dialogService = inject(DialogService);
  private toastService = inject(ToastService);
  private translocoService = inject(TranslocoService);

  private destroy$ = new Subject<void>();

  building = signal<BuildingDetailResponse | null>(null);
  saving = signal(false);
  loading = signal(false);
  buildingId = signal<string>('');
  propertyName = signal<string>('');
  selectedTab = signal<string>('details');

  detailsConfig = computed<EntityDetailsConfig>(() => {
    const building = this.building();
    return {
      title: this.translocoService.translate('buildings.editTitle'),
      subtitle: building?.name,
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
          handler: () => this.deleteBuilding(),
          separatorBefore: true,
        },
      ],
    };
  });

  form!: FormGroup;

  get codeControl() {
    return this.form.get('code')!;
  }

  get nameControl() {
    return this.form.get('name')!;
  }

  ngOnInit(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe((params) => {
      const id = params['id'];
      if (!id) {
        this.router.navigate(['/buildings']);
        return;
      }

      this.buildingId.set(id);
      this.initializeForm();
      this.loadBuilding(id);
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.form = this.fb.group({
      propertyId: [{ value: '', disabled: true }],
      code: ['', [Validators.required, Validators.maxLength(50)]],
      name: ['', [Validators.required, Validators.maxLength(200)]],
    });
  }

  protected loadBuilding(id: string): void {
    this.loading.set(true);

    this.buildingService
      .get(id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (building) => {
          if (!building) {
            this.toastService.error('buildings.error.loadFailed');
            this.router.navigate(['/buildings']);
            return;
          }

          this.building.set(building);
          this.form.patchValue({
            propertyId: building.propertyId,
            code: building.code,
            name: building.name,
          });
          this.form.markAsPristine();

          this.propertyService
            .get(building.propertyId)
            .pipe(map((property) => property?.name ?? building.propertyId))
            .subscribe((name) => this.propertyName.set(name));
        },
        error: () => {
          this.toastService.error('buildings.error.loadFailed');
          this.router.navigate(['/buildings']);
        },
      });
  }

  async save(): Promise<void> {
    if (this.form.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);

    const id = this.buildingId();
    const request: UpdateBuildingRequest = {
      code: this.codeControl.value,
      name: this.nameControl.value,
    };

    await firstValueFrom(
      this.buildingService.update(id, request).pipe(finalize(() => this.saving.set(false))),
    )
      .then(() => {
        this.toastService.success('buildings.success.updated');
        this.form.markAsPristine();
        this.loadBuilding(id);
      })
      .catch(() => {
        this.toastService.error('buildings.error.updateFailed');
      });
  }

  async deleteBuilding(): Promise<void> {
    const building = this.building();
    if (!building) return;

    const confirmed = await firstValueFrom(
      this.dialogService.confirm({
        title: this.translocoService.translate('common.delete'),
        description: this.translocoService.translate('buildings.deleteConfirm', {
          name: building.name,
        }),
        variant: 'destructive',
      }),
    );

    if (!confirmed) return;

    this.loading.set(true);

    this.buildingService
      .delete(building.id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => {
          this.toastService.success('buildings.success.deleted');
          this.router.navigate(['/buildings']);
        },
        error: () => {
          this.toastService.error('buildings.error.deleteFailed');
        },
      });
  }
}
