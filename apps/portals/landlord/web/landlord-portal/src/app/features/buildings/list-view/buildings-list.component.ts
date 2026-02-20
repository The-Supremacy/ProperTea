import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom, map } from 'rxjs';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ColumnDef } from '@tanstack/angular-table';
import {
  EntityAction,
  EntityListConfig,
  EntityListViewComponent,
  FilterField,
} from '../../../../shared/components/entity-list-view';
import { BuildingService } from '../services/building.service';
import { BuildingFilters, BuildingListItem } from '../models/building.models';
import { PropertyService } from '../../properties/services/property.service';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { CreateBuildingDrawerComponent } from '../create-drawer/create-building-drawer.component';

@Component({
  selector: 'app-buildings-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe, EntityListViewComponent, CreateBuildingDrawerComponent],
  template: `
    <app-entity-list-view
      [config]="listConfig()"
      [title]="'buildings.title' | transloco"
      (createClick)="createDrawerOpen.set(true)"
    />
    <app-create-building-drawer
      [open]="createDrawerOpen()"
      (openChange)="createDrawerOpen.set($event)"
    />
  `,
})
export class BuildingsListComponent {
  private buildingService = inject(BuildingService);
  private propertyService = inject(PropertyService);
  private router = inject(Router);
  private dialogService = inject(DialogService);
  private toastService = inject(ToastService);
  private translocoService = inject(TranslocoService);

  protected createDrawerOpen = signal(false);

  protected listConfig = computed<EntityListConfig<BuildingListItem, BuildingFilters>>(() => ({
    fetchFn: (query) => {
      return this.buildingService
        .list(query.filters || {}, query.pagination, query.sort)
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
      title: this.translocoService.translate('buildings.noBuildings'),
      description: this.translocoService.translate('buildings.emptyDescription'),
      icon: 'apartment',
    },
    navigation: {
      getDetailsRoute: (building) => ['/buildings', building.id],
    },
  }));

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

  private getFilterFields(): FilterField<BuildingFilters>[] {
    return [
      {
        key: 'propertyId',
        label: 'buildings.property',
        type: 'autocomplete',
        placeholder: 'common.search',
        optionsProvider: () =>
          this.propertyService.select().pipe(
            map((properties) =>
              properties.map((property) => ({ value: property.id, label: `${property.code} – ${property.name}` }))
            ),
          ),
      },
      {
        key: 'name',
        label: 'buildings.name',
        type: 'text',
        placeholder: 'buildings.searchByName',
      },
      {
        key: 'code',
        label: 'buildings.code',
        type: 'text',
        placeholder: 'buildings.searchByCode',
      },
    ];
  }

  editBuilding(id: string): void {
    this.router.navigate(['/buildings', id]);
  }

  async deleteBuilding(building: BuildingListItem): Promise<void> {
    const confirmed = await firstValueFrom(
      this.dialogService.confirm({
        title: this.translocoService.translate('buildings.deleteConfirmTitle'),
        description: this.translocoService.translate('buildings.deleteConfirmMessage', {
          name: building.name,
        }),
        confirmText: this.translocoService.translate('common.delete'),
        variant: 'destructive',
      }),
    );

    if (!confirmed) return;

    this.buildingService.delete(building.id).subscribe({
      next: () => {
        this.toastService.success('buildings.success.deleted');
      },
      error: () => {
        this.toastService.error('buildings.error.deleteFailed');
      },
    });
  }
}
