import { Component, inject, signal, computed, viewChild, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil, finalize, firstValueFrom, map } from 'rxjs';
import { DatePipe } from '@angular/common';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { PropertyService } from '../services/property.service';
import { CompanyService } from '../../companies/services/company.service';
import { PropertyDetailResponse, UpdatePropertyRequest } from '../models/property.models';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { EntityDetailsViewComponent, EntityDetailsConfig } from '../../../../shared/components/entity-details-view';
import { HlmTabsImports } from '@spartan-ng/helm/tabs';
import { HlmAccordionImports } from '@spartan-ng/helm/accordion';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmFormFieldImports } from '@spartan-ng/helm/form-field';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmLabel } from '@spartan-ng/helm/label';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmTextarea } from '@spartan-ng/helm/textarea';
import { IconComponent } from '../../../../shared/components/icon';
import { StatusBadgeDirective } from '../../../../shared/directives';
import { PropertyAuditLogComponent } from '../audit-log/property-audit-log.component';
import { BuildingsEmbeddedListComponent } from '../../buildings/embedded-list/buildings-embedded-list.component';
import { CreateBuildingDrawerComponent } from '../../buildings/create-drawer/create-building-drawer.component';

@Component({
  selector: 'app-property-details',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    TranslocoPipe,
    EntityDetailsViewComponent,
    HlmTabsImports,
    HlmAccordionImports,
    HlmSpinner,
    HlmCardImports,
    HlmFormFieldImports,
    HlmLabel,
    HlmButton,
    HlmInput,
    HlmTextarea,
    IconComponent,
    StatusBadgeDirective,
    PropertyAuditLogComponent,
    BuildingsEmbeddedListComponent,
    CreateBuildingDrawerComponent
  ],
  templateUrl: './property-details.component.html'
})
export class PropertyDetailsComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private propertyService = inject(PropertyService);
  private companyService = inject(CompanyService);
  private dialogService = inject(DialogService);
  private toastService = inject(ToastService);
  private translocoService = inject(TranslocoService);

  private destroy$ = new Subject<void>();

  // State
  property = signal<PropertyDetailResponse | null>(null);
  saving = signal(false);
  loading = signal(false);
  propertyId = signal<string>('');
  companyName = signal<string>('');
  selectedTab = signal<string>('details');
  buildingsAccordionOpen = signal(false);
  createBuildingDrawerOpen = signal(false);

  private buildingsEmbeddedList = viewChild(BuildingsEmbeddedListComponent);

  onBuildingCreated(): void {
    this.buildingsEmbeddedList()?.refresh();
  }

  // Details view configuration
  detailsConfig = computed<EntityDetailsConfig>(() => {
    const property = this.property();
    return {
      title: this.translocoService.translate('properties.editTitle'),
      subtitle: property?.name,
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
          handler: () => this.deleteProperty(),
          separatorBefore: true,
        },
      ],
    };
  });

  // Form
  form!: FormGroup;

  get codeControl() {
    return this.form.get('code')!;
  }

  get nameControl() {
    return this.form.get('name')!;
  }

  get addressControl() {
    return this.form.get('address')!;
  }

  ngOnInit(): void {
    this.route.params.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      const id = params['id'];
      if (!id) {
        this.router.navigate(['/properties']);
        return;
      }

      this.propertyId.set(id);
      this.initializeForm();
      this.loadProperty(id);
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.form = this.fb.group({
      companyId: [{ value: '', disabled: true }],
      code: ['', [Validators.required, Validators.maxLength(50)]],
      name: ['', [Validators.required, Validators.maxLength(200)]],
      address: ['', [Validators.required, Validators.maxLength(500)]]
    });
  }

  protected loadProperty(id: string): void {
    this.loading.set(true);

    this.propertyService.get(id).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: (property) => {
        if (property) {
          this.property.set(property);
          this.form.patchValue({
            companyId: property.companyId,
            code: property.code,
            name: property.name,
            address: property.address
          });
          this.form.markAsPristine();
          this.form.markAsUntouched();

          // Resolve company name for display
          this.companyService.select().pipe(
            map(companies => companies.find(c => c.id === property.companyId)?.name ?? property.companyId)
          ).subscribe(name => this.companyName.set(name));
        } else {
          this.toastService.error('properties.error.loadFailed');
          this.router.navigate(['/properties']);
        }
      },
      error: () => {
        this.toastService.error('properties.error.loadFailed');
        this.router.navigate(['/properties']);
      }
    });
  }

  async save(): Promise<void> {
    if (this.form.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);

    const id = this.propertyId();
    const request: UpdatePropertyRequest = {
      code: this.codeControl.value,
      name: this.nameControl.value,
      address: this.addressControl.value
    };

    await firstValueFrom(
      this.propertyService.update(id, request).pipe(
        finalize(() => this.saving.set(false))
      )
    ).then(() => {
      this.toastService.success('properties.success.updated');
      this.form.markAsPristine();
      this.loadProperty(id);
    }).catch(() => {
      this.toastService.error('properties.error.updateFailed');
    });
  }

  async deleteProperty(): Promise<void> {
    const property = this.property();
    if (!property) return;

    const title = this.translocoService.translate('common.delete');
    const description = this.translocoService.translate('properties.deleteConfirm', { name: property.name });

    const confirmed = await firstValueFrom(this.dialogService.confirm({
      title,
      description,
      variant: 'destructive'
    }));

    if (!confirmed) return;

    this.loading.set(true);

    this.propertyService.delete(property.id).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: () => {
        this.toastService.success('properties.success.deleted');
        this.router.navigate(['/properties']);
      },
      error: () => {
        this.toastService.error('properties.error.deleteFailed');
      }
    });
  }
}
