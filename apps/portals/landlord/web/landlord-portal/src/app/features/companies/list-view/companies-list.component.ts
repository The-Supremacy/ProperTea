import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { finalize, firstValueFrom, map } from 'rxjs';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ColumnDef } from '@tanstack/angular-table';
import { CompanyService } from '../services/company.service';
import { CompanyListItem, CompanyFilters } from '../models/company.models';
import { EntityListViewComponent, EntityListConfig, EntityAction, FilterField, TableAction } from '../../../../shared/components/entity-list-view';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-companies-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe, EntityListViewComponent],
  templateUrl: './companies-list.component.html',
  styleUrl: './companies-list.component.css',
})
export class CompaniesListComponent {
  private companyService = inject(CompanyService);
  private router = inject(Router);
  private dialogService = inject(DialogService);
  private toastService = inject(ToastService);
  private translocoService = inject(TranslocoService);

  // UI state
  protected createDrawerOpen = signal(false);

  // Entity list configuration
  protected listConfig = computed<EntityListConfig<CompanyListItem, CompanyFilters>>(() => ({
    fetchFn: (query) =>
      this.companyService.list(query.filters || {}, query.pagination, query.sort).pipe(
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
    tableActions: this.getTableActions(),
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
      title: this.translocoService.translate('companies.noCompanies'),
      description: this.translocoService.translate('companies.noCompaniesDescription'),
      icon: 'business',
    },
  }));

  private getColumnDefinitions(): ColumnDef<CompanyListItem>[] {
    return [
      {
        id: 'name',
        header: this.translocoService.translate('companies.name'),
        accessorKey: 'name',
        cell: (info) => info.getValue(),
        enableSorting: true,
      },
      {
        id: 'status',
        header: this.translocoService.translate('companies.status'),
        accessorKey: 'status',
        cell: (info) => {
          const status = info.getValue() as string;
          const isActive = status === 'Active';
          const variantClass = isActive
            ? 'inline-flex rounded-full bg-green-100 px-2 py-1 text-xs font-semibold text-green-800 dark:bg-green-900 dark:text-green-200'
            : 'inline-flex rounded-full bg-gray-100 px-2 py-1 text-xs font-semibold text-gray-800 dark:bg-gray-800 dark:text-gray-200';
          const translatedStatus = this.translocoService.translate(
            'companies.' + status.toLowerCase(),
          );
          return `<span class="${variantClass}">${translatedStatus}</span>`;
        },
        meta: { useInnerHTML: true },
        enableSorting: false,
      },
      {
        id: 'createdAt',
        header: this.translocoService.translate('companies.createdAt'),
        accessorKey: 'createdAt',
        cell: (info) => {
          const date = info.getValue() as Date;
          return new Date(date).toLocaleString();
        },
        enableSorting: true,
      },
    ];
  }

  private getActions(): EntityAction<CompanyListItem>[] {
    return [
      {
        label: 'companies.edit',
        icon: 'edit',
        handler: (company) => this.editCompany(company.id),
      },
      {
        label: 'companies.delete',
        icon: 'delete',
        variant: 'destructive',
        handler: (company) => this.deleteCompany(company),
        separatorBefore: true,
      },
    ];
  }

  private getTableActions(): TableAction[] {
    return [
      {
        label: 'companies.exportAll',
        icon: 'download',
        handler: () => this.exportAll(),
      }
    ];
  }

  private getFilterFields(): FilterField<CompanyFilters>[] {
    return [
      {
        key: 'name',
        label: 'companies.name',
        type: 'text',
        placeholder: 'companies.searchByName',
      },
    ];
  }

  editCompany(id: string): void {
    this.router.navigate(['/companies', id]);
  }

  async deleteCompany(company: CompanyListItem): Promise<void> {
    const title = this.translocoService.translate('common.delete');
    const description = this.translocoService.translate('companies.deleteConfirm', {
      name: company.name,
    });

    const confirmed = await firstValueFrom(
      this.dialogService.confirm({
        title,
        description,
        variant: 'destructive',
      }),
    );

    if (!confirmed) return;

    this.companyService
      .delete(company.id)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => {
          this.toastService.success('companies.success.deleted');
        },
        error: () => {
          this.toastService.error('companies.error.deleteFailed');
        },
      });
  }

  exportAll(): void {
    this.toastService.info('companies.exportNotImplemented');
  }
}
