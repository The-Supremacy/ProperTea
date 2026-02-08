# Entity List View Component Architecture

## Overview
A reusable, headless component for displaying entity lists with built-in support for filtering, sorting, pagination, and column management. Leverages `@tanstack/angular-table` for advanced table functionality.

## Current State Analysis

### Companies List Component Issues
- Does not utilize TanStack Table's core features (column definitions, sorting state, filtering)
- Manual implementation of sorting, pagination, filtering
- Not reusable across entities
- Tightly coupled to Company domain

### What We're Building Right
- Directive-based pagination (`TablePaginationDirective`)
- Separated query models (`PaginationQuery`, `SortQuery`, `Filters`)
- Local loading states
- Filter drawer pattern
- Mobile-responsive (table for desktop, cards for mobile)

## Proposed Architecture

### Component Structure
```
src/shared/components/entity-list-view/
├── entity-list-view.component.ts         # Main container component
├── entity-list-view.component.html       # Template with slots/ng-content
├── entity-list-view.component.css        # Styling
├── entity-list-view.models.ts            # Interfaces and types
├── column-drawer/                        # Column selection drawer
│   ├── column-drawer.component.ts
│   └── column-drawer.component.html
└── index.ts                              # Barrel export
```

### Core Concepts

#### 1. Generic Entity Type
```typescript
export interface EntityListConfig<TEntity, TFilters> {
  // Data fetching
  fetchFn: (query: EntityListQuery<TFilters>) => Observable<PagedResult<TEntity>>;

  // TanStack Table column definitions
  columns: ColumnDef<TEntity>[];

  // Unique identifier field
  idField: keyof TEntity;

  // Mobile card template reference
  mobileCardTemplate?: TemplateRef<{ $implicit: TEntity }>;

  // Row actions
  actions?: EntityAction<TEntity>[];

  // Filtering
  filterConfig?: FilterConfig<TFilters>;

  // Initial state
  initialPageSize?: number;
  initialSort?: SortQuery;
  initialFilters?: TFilters;

  // Feature flags
  features?: {
    search?: boolean;
    filters?: boolean;
    columnSelection?: boolean;
    export?: boolean;
    refresh?: boolean;
  };
}

export interface EntityListQuery<TFilters> {
  pagination: PaginationQuery;
  sort?: SortQuery;
  filters?: TFilters;
}

export interface EntityAction<TEntity> {
  label: string;
  icon: string;
  handler: (entity: TEntity) => void | Promise<void>;
  variant?: 'default' | 'destructive';
  condition?: (entity: TEntity) => boolean; // Show only if true
}
```

#### 2. TanStack Table Integration
```typescript
@Component({
  selector: 'app-entity-list-view',
  // ...
})
export class EntityListViewComponent<TEntity, TFilters> implements OnInit {
  // TanStack Table instance
  private table = createAngularTable(() => ({
    data: this.data(),
    columns: this.config().columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    onSortingChange: updater => this.handleSortChange(updater),
    onColumnVisibilityChange: updater => this.handleColumnVisibilityChange(updater),
    state: {
      sorting: this.sortingState(),
      columnVisibility: this.columnVisibility(),
    },
    manualPagination: true, // Server-side pagination
    pageCount: this.totalPages(),
  }));

  // Signals
  config = input.required<EntityListConfig<TEntity, TFilters>>();
  data = signal<TEntity[]>([]);
  loading = signal(false);
  sortingState = signal<SortingState>([]);
  columnVisibility = signal<VisibilityState>({});

  // Query state
  private paginationQuery = signal<PaginationQuery>({ page: 1, pageSize: 20 });
  private sortQuery = signal<SortQuery | undefined>(undefined);
  private filtersQuery = signal<TFilters | undefined>(undefined);

  // Computed query combining all parameters
  private query = computed<EntityListQuery<TFilters>>(() => ({
    pagination: this.paginationQuery(),
    sort: this.sortQuery(),
    filters: this.filtersQuery(),
  }));

  ngOnInit() {
    // React to query changes and fetch data
    effect(() => {
      this.loadData(this.query());
    });
  }

  private loadData(query: EntityListQuery<TFilters>) {
    this.loading.set(true);
    this.config().fetchFn(query)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(result => {
        this.data.set(result.items);
        this.totalCount.set(result.totalCount);
      });
  }
}
```

#### 3. Filter Drawer System
```typescript
export interface FilterConfig<TFilters> {
  // Filter field definitions
  fields: FilterField<TFilters>[];

  // Template for custom filter UI (optional)
  customTemplate?: TemplateRef<{ filters: Signal<TFilters> }>;
}

export interface FilterField<TFilters> {
  key: keyof TFilters;
  label: string;
  type: 'text' | 'select' | 'date' | 'dateRange' | 'number' | 'boolean';
  placeholder?: string;
  options?: { label: string; value: any }[]; // For select type
  debounce?: number; // For text type (default 500ms)
}
```

#### 4. Column Selection Drawer
- Checkbox list of all columns
- Drag-and-drop reordering (using Angular CDK)
- Save preferences to localStorage (keyed by entity type)
- "Reset to Default" button

```typescript
export interface ColumnPreferences {
  visibility: Record<string, boolean>;
  order: string[]; // Column IDs
}
```

#### 5. Usage Example
```typescript
// companies-list.component.ts
export class CompaniesListComponent implements OnInit {
  private companyService = inject(CompanyService);

  protected config = computed<EntityListConfig<CompanyListItem, CompanyFilters>>(() => ({
    fetchFn: (query) => this.companyService.list(query.pagination, query.sort, query.filters),

    columns: [
      {
        id: 'name',
        accessorKey: 'name',
        header: 'Company Name',
        cell: (info) => info.getValue(),
        enableSorting: true,
      },
      {
        id: 'status',
        accessorKey: 'status',
        header: 'Status',
        cell: (info) => this.renderStatusBadge(info.getValue()),
      },
      {
        id: 'createdAt',
        accessorKey: 'createdAt',
        header: 'Created',
        cell: (info) => this.datePipe.transform(info.getValue(), 'medium'),
        enableSorting: true,
      },
    ],

    idField: 'id',

    actions: [
      {
        label: 'common.edit',
        icon: 'edit',
        handler: (company) => this.router.navigate(['/companies', company.id]),
      },
      {
        label: 'common.delete',
        icon: 'delete',
        handler: (company) => this.deleteCompany(company),
        variant: 'destructive',
      },
    ],

    filterConfig: {
      fields: [
        {
          key: 'name',
          label: 'companies.name',
          type: 'text',
          placeholder: 'companies.namePlaceholder',
          debounce: 500,
        },
        // Future filters...
      ],
    },

    initialPageSize: 20,
    initialSort: { field: 'createdAt', direction: 'desc' },

    features: {
      search: true,
      filters: true,
      columnSelection: true,
      refresh: true,
    },
  }));
}
```

```html
<!-- companies-list.component.html -->
<app-entity-list-view
  [config]="config()"
  [title]="'companies.title' | transloco"
  [createRoute]="'/companies/new'"
  [createLabel]="'common.new' | transloco">

  <!-- Custom mobile card template -->
  <ng-template #mobileCard let-company>
    <div class="p-4 border rounded-lg">
      <h3 class="font-semibold">{{ company.name }}</h3>
      <p class="text-sm text-muted-foreground">
        {{ company.status }} • {{ company.createdAt | date }}
      </p>
    </div>
  </ng-template>
</app-entity-list-view>
```

## Implementation Phases

### Phase 1: Foundation
- [ ] Create base EntityListViewComponent with generics
- [ ] Integrate TanStack Table with sorting/filtering
- [ ] Implement server-side pagination integration
- [ ] Add loading states and error handling

### Phase 2: Core Features
- [ ] Filter drawer with dynamic field generation
- [ ] Actions menu system
- [ ] Mobile responsive with template slots
- [ ] Toolbar with standard actions (create, refresh, filters)

### Phase 3: Advanced Features
- [ ] Column selection drawer with visibility toggle
- [ ] Column reordering with Angular CDK drag-drop
- [ ] Column preferences persistence (localStorage)
- [ ] Export functionality (CSV, Excel)

### Phase 4: Polish & Optimization
- [ ] Virtual scrolling for large datasets
- [ ] Keyboard navigation
- [ ] Accessibility improvements (ARIA, focus management)
- [ ] Performance optimization (memoization, lazy rendering)

### Phase 5: Migration
- [ ] Migrate CompaniesListComponent to use EntityListView
- [ ] Extract learnings and refine API
- [ ] Documentation and usage examples
- [ ] Prepare for other entity types

## Benefits

### For Developers
- **Consistency** - All list views follow same patterns
- **Less boilerplate** - No repeated code for common features
- **Type safety** - Full TypeScript generics support
- **Testability** - Isolated, well-defined component

### For Users
- **Uniform UX** - Same interactions across all entities
- **Power features** - Column selection, advanced filtering
- **Performance** - Optimized rendering with TanStack Table
- **Accessibility** - WCAG AA compliance built-in

## TanStack Table Benefits We'll Leverage

1. **Column Management**
   - Built-in visibility toggle
   - Resizing capabilities
   - Sorting state management

2. **Performance**
   - Virtual rendering for large datasets
   - Memoized row rendering
   - Efficient updates

3. **Filtering**
   - Column-level filtering
   - Global filtering
   - Custom filter functions

4. **Extensibility**
   - Plugin system
   - Custom cell renderers
   - Row selection support

## Migration Strategy

1. **Extract** - Move pagination, sorting, filtering logic out of CompaniesListComponent
2. **Generalize** - Remove company-specific dependencies
3. **Test** - Ensure existing functionality works with new component
4. **Refine** - Improve API based on first migration
5. **Document** - Create usage guide and examples
6. **Expand** - Apply to other entity types

## Open Questions

1. **Search vs Filter** - Should search be in drawer or toolbar? (Current: Drawer)
2. **Column defaults** - Should defaults be per-user or per-entity-type?
3. **Bulk actions** - Do we need row selection for bulk operations?
4. **Export format** - CSV only, or also Excel/PDF?
5. **Virtualization** - When to enable? (e.g., > 100 rows?)

## References

- [TanStack Table Docs](https://tanstack.com/table/latest)
- [Angular CDK Drag Drop](https://material.angular.io/cdk/drag-drop/overview)
- Decision: [0011-entity-management-ui-patterns.md](../decisions/0011-entity-management-ui-patterns.md)
- Decision: [0012-headless-ui-component-strategy.md](../decisions/0012-headless-ui-component-strategy.md)
