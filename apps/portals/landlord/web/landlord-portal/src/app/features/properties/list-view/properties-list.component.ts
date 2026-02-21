import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { map, firstValueFrom } from 'rxjs';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ColumnDef } from '@tanstack/angular-table';
import { PropertyService } from '../services/property.service';
import { statusBadgeClasses } from '../../../../utils/status-badge-classes';
import { CompanyService } from '../../companies/services/company.service';
import { PropertyListItem, PropertyFilters, PropertyAddress, formatAddress } from '../models/property.models';
import {
  EntityListViewComponent,
  EntityListConfig,
  EntityAction,
  FilterField,
} from '../../../../shared/components/entity-list-view';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { CreatePropertyDrawerComponent } from '../create-drawer/create-property-drawer.component';

@Component({
  selector: 'app-properties-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe, EntityListViewComponent, CreatePropertyDrawerComponent],
  template: `
    <app-entity-list-view
      [config]="listConfig()"
      [title]="'properties.title' | transloco"
      [createLabel]="'common.new'"
      (createClick)="openCreateDrawer()"
    />

    <app-create-property-drawer
      [open]="createDrawerOpen()"
      (openChange)="createDrawerOpen.set($event)"
    />
  `,
})
export class PropertiesListComponent {
  private propertyService = inject(PropertyService);
  private companyService = inject(CompanyService);
  private router = inject(Router);
  private dialogService = inject(DialogService);
  private toastService = inject(ToastService);
  private translocoService = inject(TranslocoService);

  // UI state
  protected createDrawerOpen = signal(false);

  // Entity list configuration
  protected listConfig = computed<EntityListConfig<PropertyListItem, PropertyFilters>>(() => ({
    fetchFn: (query) =>
      this.propertyService.list(query.filters || {}, query.pagination, query.sort).pipe(
        map((response) => ({
          items: response.items,
          totalCount: response.totalCount,
          page: query.pagination.page,
          pageSize: query.pagination.pageSize,
        })),
      ),
    idField: 'id',
    columns: this.getColumnDefinitions(),
    actions: this.getActions(),
    filterConfig: {
      fields: this.getFilterFields(),
    },
    initialPageSize: 20,
    initialSort: { field: 'createdAt', descending: true },
    features: {
      search: true,
      filters: true,
      columnSelection: true,
      export: false,
      refresh: true,
      create: true,
    },
    emptyState: {
      title: this.translocoService.translate('properties.noProperties'),
      description: this.translocoService.translate('properties.noPropertiesDescription'),
      icon: 'apartment',
    },
    navigation: {
      getDetailsRoute: (property) => ['/properties', property.id],
    },
  }));

  private getColumnDefinitions(): ColumnDef<PropertyListItem>[] {
    return [
      {
        id: 'code',
        header: this.translocoService.translate('properties.code'),
        accessorKey: 'code',
        cell: (info) => info.getValue(),
        enableSorting: true,
      },
      {
        id: 'name',
        header: this.translocoService.translate('properties.name'),
        accessorKey: 'name',
        cell: (info) => info.getValue(),
        enableSorting: true,
      },
      {
        id: 'companyName',
        header: this.translocoService.translate('properties.company'),
        accessorKey: 'companyName',
        cell: (info) => info.getValue() || '-',
        enableSorting: false,
      },
      {
        id: 'address',
        header: this.translocoService.translate('properties.address'),
        accessorKey: 'address',
        cell: (info) => formatAddress(info.getValue() as PropertyAddress),
        enableSorting: true,
      },
      {
        id: 'buildingCount',
        header: this.translocoService.translate('properties.buildingCount'),
        accessorKey: 'buildingCount',
        cell: (info) => info.getValue(),
        enableSorting: false,
      },
      {
        id: 'status',
        header: this.translocoService.translate('properties.status'),
        accessorKey: 'status',
        cell: (info) => {
          const status = info.getValue() as string;
          const classes = statusBadgeClasses({ status: status === 'Active' ? 'active' : 'inactive' });
          const translatedStatus = this.translocoService.translate(
            `properties.${status.toLowerCase()}`,
          );
          return `<span class="${classes}">${translatedStatus}</span>`;
        },
        enableSorting: false,
      },
      {
        id: 'createdAt',
        header: this.translocoService.translate('properties.createdAt'),
        accessorKey: 'createdAt',
        cell: (info) => {
          const date = info.getValue() as Date;
          return new Date(date).toLocaleDateString();
        },
        enableSorting: true,
      },
    ];
  }

  private getActions(): EntityAction<PropertyListItem>[] {
    return [
      {
        label: 'properties.edit',
        icon: 'edit',
        handler: (property) => this.editProperty(property.id),
      },
      {
        label: 'properties.delete',
        icon: 'delete',
        variant: 'destructive',
        handler: (property) => this.deleteProperty(property),
        separatorBefore: true,
      },
    ];
  }

  private getFilterFields(): FilterField<PropertyFilters>[] {
    return [
      {
        key: 'name',
        label: 'properties.name',
        type: 'text',
        placeholder: 'properties.searchByName',
      },
      {
        key: 'code',
        label: 'properties.code',
        type: 'text',
        placeholder: 'properties.searchByCode',
      },
      {
        key: 'companyId',
        label: 'properties.company',
        type: 'autocomplete',
        placeholder: 'common.search',
        optionsProvider: () =>
          this.companyService.select().pipe(
            map((companies) => companies.map((company) => ({ value: company.id, label: `${company.code} – ${company.name}` }))),
          ),
      }
    ];
  }

  openCreateDrawer(): void {
    this.createDrawerOpen.set(true);
  }

  editProperty(id: string): void {
    this.router.navigate(['/properties', id]);
  }

  async deleteProperty(property: PropertyListItem): Promise<void> {
    const confirmed = await firstValueFrom(
      this.dialogService.confirm({
        title: this.translocoService.translate('properties.deleteConfirmTitle'),
        description: this.translocoService.translate('properties.deleteConfirmMessage', {
          name: property.name,
        }),
        confirmText: this.translocoService.translate('common.delete'),
        variant: 'destructive',
      }),
    );

    if (!confirmed) return;

    this.propertyService.delete(property.id).subscribe({
      next: () => {
        this.toastService.success('properties.success.deleted');
      },
      error: () => {
        this.toastService.error('properties.error.deleteFailed');
      },
    });
  }
}
