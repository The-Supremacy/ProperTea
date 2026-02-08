import { TemplateRef } from '@angular/core';
import { Observable } from 'rxjs';
import { ColumnDef, SortingState, VisibilityState } from '@tanstack/angular-table';

export interface PaginationQuery {
  page: number;
  pageSize: number;
}

export interface SortQuery {
  field?: string;
  descending?: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PaginationMetadata {
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export function getPaginationMetadata(result: PagedResult<any>): PaginationMetadata {
  const totalPages = Math.ceil(result.totalCount / result.pageSize);
  return {
    totalPages,
    hasNextPage: result.page < totalPages,
    hasPreviousPage: result.page > 1,
  };
}

export interface EntityListConfig<TEntity, TFilters = any> {
  fetchFn: (query: EntityListQuery<TFilters>) => Observable<PagedResult<TEntity>>;
  columns: ColumnDef<TEntity>[];
  idField: keyof TEntity;
  mobileCardTemplate?: TemplateRef<{ $implicit: TEntity }>;
  actions?: EntityAction<TEntity>[];
  tableActions?: TableAction[];
  filterConfig?: FilterConfig<TFilters>;
  initialPageSize?: number;
  initialSort?: SortQuery;
  initialFilters?: TFilters;
  features?: EntityListFeatures;
  emptyState?: EmptyStateConfig;
}
export interface EntityListQuery<TFilters = any> {
  pagination: PaginationQuery;
  sort?: SortQuery;
  filters?: TFilters;
}

export interface EntityAction<TEntity> {
  label: string;
  icon: string;
  handler: (entity: TEntity) => void | Promise<void>;
  variant?: 'default' | 'destructive';
  condition?: (entity: TEntity) => boolean;
  separatorBefore?: boolean;
}

export interface TableAction {
  label: string;
  icon: string;
  handler: () => void | Promise<void>;
  variant?: 'default' | 'destructive';
  separatorBefore?: boolean;
}

export interface FilterConfig<TFilters> {
  fields: FilterField<TFilters>[];
  customTemplate?: TemplateRef<{ filters: TFilters }>;
}

export interface FilterField<TFilters> {
  key: keyof TFilters;
  label: string;
  type: 'text' | 'select' | 'date' | 'dateRange' | 'number' | 'boolean';
  placeholder?: string;
  options?: FilterFieldOption[];
  debounce?: number;
  min?: number;
  max?: number;
}

export interface FilterFieldOption {
  label: string;
  value: any;
}
export interface EntityListFeatures {
  search?: boolean;
  filters?: boolean;
  columnSelection?: boolean;
  export?: boolean;
  refresh?: boolean;
  create?: boolean;
}

export interface EmptyStateConfig {
  title?: string;
  description?: string;
  icon?: string;
}

export interface ColumnPreferences {
  visibility: Record<string, boolean>;
  order: string[];
}

export interface EntityListState<TFilters = any> {
  data: any[];
  totalCount: number;
  loading: boolean;
  sorting: SortingState;
  columnVisibility: VisibilityState;
  pagination: PaginationQuery;
  sort?: SortQuery;
  filters?: TFilters;
  filterDrawerOpen: boolean;
  columnDrawerOpen: boolean;
}
