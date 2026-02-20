import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom, map, of } from 'rxjs';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ColumnDef } from '@tanstack/angular-table';
import {
  EntityAction,
  EntityListConfig,
  EntityListViewComponent,
  FilterField,
} from '../../../../shared/components/entity-list-view';
import { UnitService } from '../services/unit.service';
import { UnitFilters, UnitListItem, UNIT_CATEGORIES } from '../models/unit.models';
import { PropertyService } from '../../properties/services/property.service';
import { BuildingService } from '../../buildings/services/building.service';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { CreateUnitDrawerComponent } from '../create-drawer/create-unit-drawer.component';

@Component({
  selector: 'app-units-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe, EntityListViewComponent, CreateUnitDrawerComponent],
  template: `
    <app-entity-list-view
      [config]="listConfig()"
      [title]="'units.title' | transloco"
      (createClick)="createDrawerOpen.set(true)"
    />
    <app-create-unit-drawer
      [open]="createDrawerOpen()"
      (openChange)="createDrawerOpen.set($event)"
    />
  `,
})
export class UnitsListComponent {
  private unitService = inject(UnitService);
  private propertyService = inject(PropertyService);
  private buildingService = inject(BuildingService);
  private router = inject(Router);
  private dialogService = inject(DialogService);
  private toastService = inject(ToastService);
  private translocoService = inject(TranslocoService);

  protected createDrawerOpen = signal(false);

  protected listConfig = computed<EntityListConfig<UnitListItem, UnitFilters>>(() => ({
    fetchFn: (query) =>
      this.unitService
        .list(query.filters || {}, query.pagination, query.sort)
        .pipe(
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
      title: this.translocoService.translate('units.noUnits'),
      description: this.translocoService.translate('units.emptyDescription'),
      icon: 'door_front',
    },
    navigation: {
      getDetailsRoute: (unit) => ['/units', unit.id],
    },
  }));

  private getColumnDefinitions(): ColumnDef<UnitListItem>[] {
    return [
      {
        id: 'unitReference',
        header: this.translocoService.translate('units.unitReference'),
        accessorKey: 'unitReference',
        cell: (info) => info.getValue(),
        enableSorting: true,
      },
      {
        id: 'code',
        header: this.translocoService.translate('units.code'),
        accessorKey: 'code',
        cell: (info) => info.getValue(),
        enableSorting: true,
      },
      {
        id: 'category',
        header: this.translocoService.translate('units.category'),
        accessorKey: 'category',
        cell: (info) => {
          const cat = info.getValue() as string;
          return this.translocoService.translate(`units.categories.${cat.toLowerCase()}`);
        },
        enableSorting: true,
      },
      {
        id: 'floor',
        header: this.translocoService.translate('units.floor'),
        accessorKey: 'floor',
        cell: (info) => info.getValue() ?? '—',
        enableSorting: true,
      },
      {
        id: 'status',
        header: this.translocoService.translate('units.status'),
        accessorKey: 'status',
        cell: (info) => {
          const status = info.getValue() as string;
          const isActive = status === 'Active';
          const variantClass = isActive
            ? 'inline-flex rounded-full bg-green-100 px-2 py-1 text-xs font-semibold text-green-800 dark:bg-green-900 dark:text-green-200'
            : 'inline-flex rounded-full bg-gray-100 px-2 py-1 text-xs font-semibold text-gray-800 dark:bg-gray-800 dark:text-gray-200';
          const translatedStatus = this.translocoService.translate(`units.${status.toLowerCase()}`);
          return `<span class="${variantClass}">${translatedStatus}</span>`;
        },
        enableSorting: false,
      },
      {
        id: 'createdAt',
        header: this.translocoService.translate('units.createdAt'),
        accessorKey: 'createdAt',
        cell: (info) => new Date(info.getValue() as string).toLocaleDateString(),
        enableSorting: true,
      },
    ];
  }

  private getActions(): EntityAction<UnitListItem>[] {
    return [
      {
        label: 'common.view',
        icon: 'visibility',
        handler: (unit) => { void this.router.navigate(['/units', unit.id]); },
      },
      {
        label: 'common.delete',
        icon: 'delete',
        variant: 'destructive',
        handler: (unit) => this.deleteUnit(unit),
        separatorBefore: true,
      },
    ];
  }

  private getFilterFields(): FilterField<UnitFilters>[] {
    return [
      {
        key: 'propertyId',
        label: 'units.property',
        type: 'autocomplete',
        placeholder: 'common.search',
        optionsProvider: () =>
          this.propertyService.select().pipe(
            map((properties) =>
              properties.map((p) => ({ value: p.id, label: `${p.code} – ${p.name}` }))
            ),
          ),
      },
      {
        key: 'code',
        label: 'units.code',
        type: 'text',
        placeholder: 'units.searchByCode',
      },
      {
        key: 'unitReference',
        label: 'units.unitReference',
        type: 'text',
        placeholder: 'units.searchByReference',
      },
      {
        key: 'category',
        label: 'units.category',
        type: 'select',
        optionsProvider: () =>
          of(
            UNIT_CATEGORIES.map((c) => ({
              value: c,
              // Pass the translation KEY — async-select applies | transloco internally.
              label: `units.categories.${c.toLowerCase()}`,
            })),
          ),
      },
      {
        key: 'floor',
        label: 'units.floor',
        type: 'number',
        placeholder: 'units.filterByFloor',
      },
    ];
  }

  async deleteUnit(unit: UnitListItem): Promise<void> {
    const confirmed = await firstValueFrom(
      this.dialogService.confirm({
        title: this.translocoService.translate('units.deleteConfirmTitle'),
        description: this.translocoService.translate('units.deleteConfirmMessage', {
          code: unit.code,
        }),
        confirmText: this.translocoService.translate('common.delete'),
        variant: 'destructive',
      }),
    );

    if (!confirmed) return;

    this.unitService.delete(unit.id).subscribe({
      next: () => {
        this.toastService.success('units.success.deleted');
      },
      error: () => {
        this.toastService.error('units.error.deleteFailed');
      },
    });
  }
}
