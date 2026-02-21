import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom, map } from 'rxjs';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ColumnDef } from '@tanstack/angular-table';
import { CompanyService } from '../services/company.service';
import { statusBadgeClasses } from '../../../../utils/status-badge-classes';
import { CompanyListItem, CompanyFilters } from '../models/company.models';
import { EntityListViewComponent, EntityListConfig, EntityAction, FilterField, TableAction } from '../../../../shared/components/entity-list-view';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { CreateCompanyDrawerComponent } from '../create-drawer/create-company-drawer.component';

@Component({
  selector: 'app-companies-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe, EntityListViewComponent, CreateCompanyDrawerComponent],
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
    navigation: {
      getDetailsRoute: (company) => ['/companies', company.id],
    },
  }));

  private getColumnDefinitions(): ColumnDef<CompanyListItem>[] {
    return [
      {
        id: 'code',
        header: this.translocoService.translate('companies.code'),
        accessorKey: 'code',
        cell: (info) => info.getValue(),
        enableSorting: true,
      },
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
          const classes = statusBadgeClasses({ status: status === 'Active' ? 'active' : 'inactive' });
          const translatedStatus = this.translocoService.translate(
            'companies.' + status.toLowerCase(),
          );
          return `<span class="${classes}">${translatedStatus}</span>`;
        },
        enableSorting: false,
      },
      {
        id: 'createdAt',
        header: this.translocoService.translate('companies.createdAt'),
        accessorKey: 'createdAt',
        cell: (info) => {
          const date = info.getValue() as Date;
          return new Date(date).toLocaleDateString();
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
        key: 'code',
        label: 'companies.code',
        type: 'text',
        placeholder: 'companies.searchByCode',
      },
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

  openCreateDrawer(): void {
    this.createDrawerOpen.set(true);
  }
}
