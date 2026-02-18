import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
  effect,
  inject,
  contentChild,
  viewChild,
  TemplateRef,
  OnInit,
  OnDestroy,
} from '@angular/core';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Subject, finalize, takeUntil } from 'rxjs';
import {
  createAngularTable,
  FlexRenderDirective,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
  SortingState,
  VisibilityState,
  ColumnDef,
} from '@tanstack/angular-table';
import { TranslocoPipe } from '@jsverse/transloco';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';
import { HlmSheetImports } from '@spartan-ng/helm/sheet';
import { HlmTableImports } from '@spartan-ng/helm/table';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmEmptyImports } from '@spartan-ng/helm/empty';

import {
  EntityListConfig,
  EntityListQuery,
  EntityListState,
  EntityAction,
  TableAction,
  PaginationQuery,
  SortQuery,
  PagedResult,
  getPaginationMetadata,
  FilterField,
} from './entity-list-view.models';
import { TablePaginationDirective } from '../../directives/table-pagination';
import { HlmButton } from '@spartan-ng/helm/button';
import { IconComponent } from '../icon';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { AsyncSelectComponent } from '../async-select';
import { AutocompleteComponent } from '../autocomplete';
import { ResponsiveService } from '../../../app/core/services/responsive.service';

/**
 * Generic entity list view component with TanStack Table integration.
 * Provides filtering, sorting, pagination, column management, and actions.
 *
 * @template TEntity - The entity type being displayed
 * @template TFilters - The filters type (optional)
 *
 * @example
 * ```typescript
 * <app-entity-list-view
 *   [config]="listConfig()"
 *   [title]="'companies.title' | transloco"
 *   [createRoute]="'/companies/new'"
 *   [createLabel]="'common.new' | transloco">
 *
 *   <ng-template #mobileCard let-item>
 *     <!-- Custom mobile card template -->
 *   </ng-template>
 * </app-entity-list-view>
 * ```
 */
@Component({
  selector: 'app-entity-list-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    HlmDropdownMenuImports,
    HlmSheetImports,
    HlmTableImports,
    HlmInput,
    HlmEmptyImports,
    RouterLink,
    NgTemplateOutlet,
    TranslocoPipe,
    FlexRenderDirective,
    TablePaginationDirective,
    HlmButton,
    IconComponent,
    HlmSpinner,
    AsyncSelectComponent,
    AutocompleteComponent,
  ],
  templateUrl: './entity-list-view.component.html',
  styleUrl: './entity-list-view.component.css',
})
export class EntityListViewComponent<TEntity, TFilters = any> implements OnInit, OnDestroy {
  // Services
  protected responsive = inject(ResponsiveService);
  private router = inject(Router);

  // Inputs
  config = input<EntityListConfig<TEntity, TFilters>>();
  title = input<string>();
  createRoute = input<string>();
  createLabel = input<string>('common.new');

  // Outputs
  rowClick = output<TEntity>();
  actionClick = output<{ action: string; entity: TEntity }>();
  createClick = output<void>();
  tableActionClick = output<string>();

  // Content children
  protected mobileCardTemplate = contentChild<TemplateRef<{ $implicit: TEntity }>>('mobileCard');



  // State signals
  private data = signal<TEntity[]>([]);
  private totalCount = signal(0);
  private loading = signal(false);
  private sortingState = signal<SortingState>([]);
  private columnVisibility = signal<VisibilityState>({});
  private filterDrawerOpen = signal(false);
  private columnDrawerOpen = signal(false);
  protected searchExpanded = signal(false);

  // Query state
  private paginationQuery = signal<PaginationQuery>({ page: 1, pageSize: 20 });
  private sortQuery = signal<SortQuery | undefined>(undefined);
  private filtersQuery = signal<TFilters | undefined>(undefined);

  // Filter drawer state (uncommitted until "Apply" is clicked)
  protected filterValues = signal<Partial<TFilters>>({});

  // Toolbar search (client-side filtering)
  protected globalFilter = signal('');

  // Destroy subject
  private destroy$ = new Subject<void>();

  // Computed signals
  protected hasData = computed(() => this.data().length > 0);
  protected isEmpty = computed(() => !this.loading() && !this.hasData());
  protected totalPages = computed(() =>
    Math.ceil(this.totalCount() / this.paginationQuery().pageSize),
  );

  // Combined query for data fetching
  private query = computed<EntityListQuery<TFilters>>(() => ({
    pagination: this.paginationQuery(),
    sort: this.sortQuery(),
    filters: this.filtersQuery(),
  }));

  // TanStack Table instance
  protected table = createAngularTable<TEntity>(() => {
    const cfg = this.config();
    return {
      data: this.data(),
      columns: cfg?.columns || [],
      getCoreRowModel: getCoreRowModel(),
      getSortedRowModel: getSortedRowModel(),
      getFilteredRowModel: getFilteredRowModel(),
      onSortingChange: (updater) => this.handleSortingChange(updater),
      onColumnVisibilityChange: (updater) => this.handleColumnVisibilityChange(updater),
      onGlobalFilterChange: (updater) => this.handleGlobalFilterChange(updater),
      state: {
        sorting: this.sortingState(),
        columnVisibility: this.columnVisibility(),
        globalFilter: this.globalFilter(),
      },
      manualPagination: true,
      manualSorting: true,
      pageCount: this.totalPages(),
    };
  });

  // Feature flags with defaults
  protected features = computed(() => {
    const cfg = this.config();
    const hasFilters = cfg?.features?.filters ?? true;
    return {
      search: cfg?.features?.search ?? true,
      filters: hasFilters,
      columnSelection: cfg?.features?.columnSelection ?? false,
      export: cfg?.features?.export ?? false,
      refresh: hasFilters, // Auto-enable refresh when filters are enabled
      create: cfg?.features?.create ?? true,
    };
  });

  constructor() {
    // Setup data loading effect in constructor (injection context)
    effect(() => {
      const cfg = this.config();
      if (cfg) {
        this.loadData(this.query());
      }
    });
  }

  ngOnInit(): void {
    const cfg = this.config();
    if (!cfg) return;

    if (cfg.initialPageSize) {
      this.paginationQuery.update((p) => ({ ...p, pageSize: cfg.initialPageSize! }));
    }
    if (cfg.initialSort) {
      this.sortQuery.set(cfg.initialSort);
      this.sortingState.set([
        { id: cfg.initialSort.field || '', desc: cfg.initialSort.descending || false },
      ]);
    }
    if (cfg.initialFilters) {
      this.filtersQuery.set(cfg.initialFilters);
      this.filterValues.set(cfg.initialFilters);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadData(query: EntityListQuery<TFilters>): void {
    const cfg = this.config();
    if (!cfg) return;

    this.loading.set(true);

    cfg
      .fetchFn(query)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: (result: PagedResult<TEntity>) => {
          this.data.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: (error: any) => {
          console.error('Failed to load entity list:', error);
          this.data.set([]);
          this.totalCount.set(0);
        },
      });
  }

  private handleSortingChange(updater: any): void {
    const newSorting = typeof updater === 'function' ? updater(this.sortingState()) : updater;
    this.sortingState.set(newSorting);

    if (newSorting.length > 0) {
      const sort = newSorting[0];
      this.sortQuery.set({
        field: sort.id,
        descending: sort.desc,
      });
    } else {
      this.sortQuery.set(undefined);
    }

    this.paginationQuery.update((p) => ({ ...p, page: 1 }));
  }

  private handleColumnVisibilityChange(updater: any): void {
    const newVisibility =
      typeof updater === 'function' ? updater(this.columnVisibility()) : updater;
    this.columnVisibility.set(newVisibility);

    // TODO: Persist to localStorage
  }

  protected onPageChange(page: number): void {
    this.paginationQuery.update((p) => ({ ...p, page }));
  }

  private handleGlobalFilterChange(updater: any): void {
    const newFilter = typeof updater === 'function' ? updater(this.globalFilter()) : updater;
    this.globalFilter.set(newFilter);
  }

  protected onTableSearchChange(value: string): void {
    this.globalFilter.set(value);
  }

  protected clearTableSearch(): void {
    this.globalFilter.set('');
  }

  protected onFilterFieldChange(key: keyof TFilters, value: any): void {
    this.filterValues.update((current) => ({
      ...current,
      [key]: value,
    }));
  }

  protected applyFilters(): void {
    this.filtersQuery.set(this.filterValues() as TFilters);
    this.paginationQuery.update((p) => ({ ...p, page: 1 }));
    this.toggleFilterDrawer();
  }

  protected clearFilters(): void {
    const cfg = this.config();
    const initial = cfg?.initialFilters || ({} as Partial<TFilters>);
    this.filterValues.set(initial);
    this.filtersQuery.set(initial as TFilters);
    this.paginationQuery.update((p) => ({ ...p, page: 1 }));
  }

  protected toggleFilterDrawer(): void {
    this.filterDrawerOpen.update((open) => !open);
  }

  protected closeFilterDrawer(): void {
    this.filterDrawerOpen.set(false);
  }

  protected toggleColumnDrawer(): void {
    this.columnDrawerOpen.update((open) => !open);
  }

  protected closeColumnDrawer(): void {
    this.columnDrawerOpen.set(false);
  }

  protected resetColumnVisibility(): void {
    this.columnVisibility.set({});
  }

  protected refresh(): void {
    this.loadData(this.query());
  }

  protected async handleAction(action: EntityAction<TEntity>, entity: TEntity): Promise<void> {
    await action.handler(entity);
    this.actionClick.emit({ action: action.label, entity });
  }

  protected async handleTableAction(action: TableAction): Promise<void> {
    await action.handler();
    this.tableActionClick.emit(action.label);
  }

  protected shouldShowAction(action: EntityAction<TEntity>, entity: TEntity): boolean {
    return !action.condition || action.condition(entity);
  }

  protected shouldUseInnerHTML(columnId: string): boolean {
    const column = this.config()?.columns?.find((col) => col.id === columnId);
    return !!(column?.meta as any)?.useInnerHTML;
  }

  protected getCellContent(cell: any): string {
    if (typeof cell.column.columnDef.cell === 'function') {
      return cell.column.columnDef.cell(cell.getContext());
    }
    return cell.getValue() ?? '';
  }

  protected onRowClick(entity: TEntity): void {
    const cfg = this.config();

    // If navigation config is provided, use it for automatic routing
    if (cfg?.navigation) {
      const route = cfg.navigation.getDetailsRoute(entity);
      this.router.navigate(route);
    } else {
      // Otherwise emit event for manual handling
      this.rowClick.emit(entity);
    }
  }

  protected getSortIcon(columnId: string): string {
    const sort = this.sortingState().find((s) => s.id === columnId);
    if (!sort) return 'unfold_more';
    return sort.desc ? 'arrow_downward' : 'arrow_upward';
  }

  protected isColumnSortable(column: ColumnDef<TEntity>): boolean {
    return column.enableSorting !== false;
  }

  protected getEntityId(entity: TEntity): any {
    const cfg = this.config();
    return entity[cfg?.idField || ('id' as keyof TEntity)];
  }

  protected get state() {
    return {
      data: this.data(),
      loading: this.loading(),
      hasData: this.hasData(),
      isEmpty: this.isEmpty(),
      totalCount: this.totalCount(),
      filterDrawerOpen: this.filterDrawerOpen(),
      columnDrawerOpen: this.columnDrawerOpen(),
      pagination: this.paginationQuery(),
    };
  }
}
