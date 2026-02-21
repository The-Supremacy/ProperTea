import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom, map } from 'rxjs';
import { TranslocoService } from '@jsverse/transloco';
import { ColumnDef } from '@tanstack/angular-table';
import {
  EntityAction,
  EntityListConfig,
  EntityListViewComponent,
} from '../../../../shared/components/entity-list-view';
import { BuildingService } from '../services/building.service';
import { BuildingFilters, BuildingListItem } from '../models/building.models';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-buildings-embedded-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EntityListViewComponent],
  template: `
    <app-entity-list-view
      [config]="listConfig()"
    />
  `,
})
export class BuildingsEmbeddedListComponent {
  private buildingService = inject(BuildingService);
  private router = inject(Router);
  private dialogService = inject(DialogService);
  private toastService = inject(ToastService);
  private translocoService = inject(TranslocoService);

  propertyId = input.required<string>();

  private refreshTrigger = signal(0);

  public refresh(): void {
    this.refreshTrigger.update((n) => n + 1);
  }

  protected listConfig = computed<EntityListConfig<BuildingListItem, BuildingFilters>>(() => {
    this.refreshTrigger(); // tracked so refresh() causes a reload
    const filters: BuildingFilters = {
      propertyId: this.propertyId(),
    };

    return {
      fetchFn: (query) => {
        return this.buildingService
          .list({ ...filters, ...query.filters }, query.pagination, query.sort)
          .pipe(
            map((response) => ({
              items: response.items,
              totalCount: response.totalCount,
              page: query.pagination.page,
              pageSize: query.pagination.pageSize,
            })),
          );
      },
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
        title: this.translocoService.translate('buildings.noBuildings'),
        description: this.translocoService.translate('buildings.noBuildingsInProperty'),
        icon: 'apartment',
      },
      navigation: {
        getDetailsRoute: (building) => ['/buildings', building.id],
      },
    };
  });

  private getColumnDefinitions(): ColumnDef<BuildingListItem>[] {
    return [
      {
        id: 'code',
        header: this.translocoService.translate('buildings.code'),
        accessorKey: 'code',
        cell: (info) => info.getValue(),
        enableSorting: true,
      },
      {
        id: 'name',
        header: this.translocoService.translate('buildings.name'),
        accessorKey: 'name',
        cell: (info) => info.getValue(),
        enableSorting: true,
      },
      {
        id: 'status',
        header: this.translocoService.translate('buildings.status'),
        accessorKey: 'status',
        cell: (info) => {
          const status = info.getValue() as string;
          const isActive = status === 'Active';
          const variantClass = isActive
            ? 'inline-flex rounded-full bg-green-100 px-2 py-1 text-xs font-semibold text-green-800 dark:bg-green-900 dark:text-green-200'
            : 'inline-flex rounded-full bg-gray-100 px-2 py-1 text-xs font-semibold text-gray-800 dark:bg-gray-800 dark:text-gray-200';
          const translatedStatus = this.translocoService.translate(
            `buildings.${status.toLowerCase()}`,
          );
          return `<span class="${variantClass}">${translatedStatus}</span>`;
        },
        enableSorting: false,
      },
      {
        id: 'createdAt',
        header: this.translocoService.translate('buildings.createdAt'),
        accessorKey: 'createdAt',
        cell: (info) => {
          const date = info.getValue() as Date;
          return new Date(date).toLocaleDateString();
        },
        enableSorting: true,
      },
    ];
  }

  private getActions(): EntityAction<BuildingListItem>[] {
    return [
      {
        label: 'buildings.edit',
        icon: 'edit',
        handler: (building) => this.editBuilding(building.id),
      },
      {
        label: 'buildings.delete',
        icon: 'delete',
        variant: 'destructive',
        handler: (building) => this.deleteBuilding(building),
        separatorBefore: true,
      },
    ];
  }

  private editBuilding(id: string): void {
    this.router.navigate(['/buildings', id]);
  }

  private async deleteBuilding(building: BuildingListItem): Promise<void> {
    const confirmed = await firstValueFrom(
      this.dialogService.confirm({
        title: this.translocoService.translate('buildings.deleteConfirmTitle'),
        description: this.translocoService.translate('buildings.deleteConfirmMessage', {
          name: building.name,
        }),
        confirmText: this.translocoService.translate('common.delete'),
        cancelText: this.translocoService.translate('common.cancel'),
        variant: 'destructive',
      }),
    );

    if (!confirmed) return;

    this.buildingService.delete(building.id).subscribe({
      next: () => {
        this.toastService.success('buildings.success.deleted');
        // The EntityListViewComponent will auto-refresh after the action
      },
      error: () => {
        this.toastService.error('buildings.error.deleteFailed');
      },
    });
  }
}
