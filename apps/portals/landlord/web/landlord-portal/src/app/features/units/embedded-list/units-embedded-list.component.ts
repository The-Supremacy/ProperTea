import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { map, of } from 'rxjs';
import { TranslocoService } from '@jsverse/transloco';
import { ColumnDef } from '@tanstack/angular-table';
import {
  EntityAction,
  EntityListConfig,
  EntityListViewComponent,
} from '../../../../shared/components/entity-list-view';
import { UnitService } from '../services/unit.service';
import { UnitFilters, UnitListItem } from '../models/unit.models';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-units-embedded-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EntityListViewComponent],
  template: `
    <app-entity-list-view [config]="listConfig()" />
  `,
})
export class UnitsEmbeddedListComponent {
  private unitService = inject(UnitService);
  private router = inject(Router);
  private dialogService = inject(DialogService);
  private toastService = inject(ToastService);
  private translocoService = inject(TranslocoService);

  propertyId = input<string | undefined>(undefined);
  buildingId = input<string | undefined>(undefined);

  private refreshTrigger = signal(0);

  public refresh(): void {
    this.refreshTrigger.update((n) => n + 1);
  }

  protected listConfig = computed<EntityListConfig<UnitListItem, UnitFilters>>(() => {
    this.refreshTrigger();
    const filters: UnitFilters = {
      propertyId: this.propertyId(),
      buildingId: this.buildingId(),
    };

    return {
      fetchFn: (query) =>
        this.unitService
          .list({ ...filters, ...query.filters }, query.pagination, query.sort)
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
      initialPageSize: 10,
      initialSort: { field: 'code', descending: false },
      features: {
        search: false,
        filters: false,
        columnSelection: false,
        export: false,
        refresh: true,
        create: false,
      },
      emptyState: {
        title: this.translocoService.translate('units.noUnits'),
        description: this.translocoService.translate('units.noUnitsInProperty'),
        icon: 'door_front',
      },
      navigation: {
        getDetailsRoute: (unit) => ['/units', unit.id],
      },
    };
  });

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
        enableSorting: false,
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
          const label = this.translocoService.translate(`units.${status.toLowerCase()}`);
          return `<span class="${variantClass}">${label}</span>`;
        },
        enableSorting: false,
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
    ];
  }
}
