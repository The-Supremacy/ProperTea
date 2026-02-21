import { Observable } from 'rxjs';
import { ColumnDef } from '@tanstack/angular-table';

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

export function getPaginationMetadata(result: PagedResult<unknown>): PaginationMetadata {
  const totalPages = Math.ceil(result.totalCount / result.pageSize);
  return {
    totalPages,
    hasNextPage: result.page < totalPages,
    hasPreviousPage: result.page > 1,
  };
}

export interface EntityListConfig<TEntity, TFilters = Record<string, unknown>> {
  fetchFn: (query: EntityListQuery<TFilters>) => Observable<PagedResult<TEntity>>;
  columns: ColumnDef<TEntity>[];
  idField: keyof TEntity;
  actions?: EntityAction<TEntity>[];
  tableActions?: TableAction[];
  filterConfig?: FilterConfig<TFilters>;
  initialPageSize?: number;
  initialSort?: SortQuery;
  initialFilters?: TFilters;
  features?: EntityListFeatures;
  emptyState?: EmptyStateConfig;
  navigation?: EntityListNavigation<TEntity>;
}
export interface EntityListQuery<TFilters = Record<string, unknown>> {
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
}

export interface FilterField<TFilters> {
  key: string & keyof TFilters;
  label: string;
  type: 'text' | 'select' | 'autocomplete' | 'date' | 'dateRange' | 'number' | 'boolean';
  placeholder?: string;
  optionsProvider?: () => Observable<FilterFieldOption[]>;
  debounce?: number;
  min?: number;
  max?: number;
}

export interface FilterFieldOption {
  label: string;
  value: string;
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

export interface EntityListNavigation<TEntity> {
  getDetailsRoute: (entity: TEntity) => unknown[];
  target?: '_self' | '_blank';
}

export interface ColumnPreferences {
  visibility: Record<string, boolean>;
  order: string[];
}
