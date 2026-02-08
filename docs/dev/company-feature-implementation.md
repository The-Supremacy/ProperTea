# Company Feature Implementation Plan

**Feature**: Complete CRUD interface for Company entity management
**Component**: Landlord Portal (Angular frontend)
**Status**: Ready for implementation
**Date**: 2026-02-06

---

## Overview

First complete entity management interface for the landlord portal, establishing patterns for all future entities (Properties, Units, Tenants, etc.). Backend and BFF are fully implemented with CRUD endpoints, pagination, filtering, sorting, and name uniqueness checking.

**Important**: Code samples in this document are approximate and will be refined during implementation. This is a living document that evolves with the actual implementation.

---

## Backend API (Already Implemented)

### Endpoints

**Base URL**: `/api/companies` (BFF proxies to Company service)

| Method | Endpoint | Purpose | Request | Response |
|--------|----------|---------|---------|----------|
| GET | `/api/companies` | List with pagination/filtering | Query params | `PagedCompaniesResponse` |
| GET | `/api/companies/{id}` | Get by ID | Path param | `CompanyDetailResponse` or 404 |
| POST | `/api/companies` | Create new | `CreateCompanyRequest` | `{ id: Guid }` with 201 |
| PUT | `/api/companies/{id}` | Update name | `UpdateCompanyNameRequest` | 204 No Content |
| DELETE | `/api/companies/{id}` | Soft delete | Path param | 204 No Content |
| GET | `/api/companies/check-name` | Check uniqueness | Query params | `CheckNameResponse` |

### Query Parameters (List Endpoint)

Query parameters are separated into three categories:

**Filters** (entity-specific):
- `name` (string, optional): Filter by company name (case-insensitive contains)

**Pagination** (reusable):
- `page` (int): Page number (1-based)
- `pageSize` (int): Items per page

**Sort** (reusable):
- `sort` (string, optional): Sort field and direction (e.g., `name`, `name:desc`, `createdAt:desc`)

### DTOs

```typescript
// ============================================
// REUSABLE BASE MODELS (shared/models/)
// ============================================

// Pagination query parameters (reusable across all entities)
interface PaginationQuery {
  page: number;
  pageSize: number;
}

// Sort query parameters (reusable across all entities)
interface SortQuery {
  field?: string;
  descending?: boolean;
}

// Generic paged result (reusable across all entities)
interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  // Computed properties (can be in getter methods)
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// ============================================
// COMPANY-SPECIFIC MODELS
// ============================================

// Request Models
interface CreateCompanyRequest {
  name: string;
}

interface UpdateCompanyNameRequest {
  name: string;
}

// Filter Models (entity-specific)
interface CompanyFilters {
  name?: string;
}

// Response Models
interface CompanyListItem {
  id: string;
  name: string;
  status: string; // "Active" | "Deleted"
  createdAt: Date; // .NET serializes as ISO 8601 string, Angular HttpClient deserializes to Date
}

interface CompanyDetailResponse {
  id: string;
  name: string;
  status: string;
  createdAt: Date;
}

// Typed paged result for companies
type PagedCompaniesResponse = PagedResult<CompanyListItem>;

interface CheckNameResponse {
  available: boolean;
  existingCompanyId?: string;
}
```

### Validation Rules

- **Name**: Required, non-empty string
- **Uniqueness**: Company names must be unique within organization (tenant)
- **Soft Delete**: Cannot update or delete already deleted companies
- **Authorization**: All endpoints require authentication, tenant ID from claims

---

## Frontend Implementation

### 1. Project Structure

```
src/shared/models/
├── pagination.models.ts      # PaginationQuery, SortQuery, PagedResult<T>
└── index.ts

src/shared/directives/
├── table-pagination/
│   ├── table-pagination.directive.ts
│   └── index.ts
└── index.ts

src/app/features/companies/
├── list-view/
│   └── companies-list.component.ts
├── details/
│   └── company-details.component.ts
├── models/
│   └── company.models.ts
├── services/
│   └── company.service.ts
└── index.ts
```

### 2. Shared Base Models (`shared/models/pagination.models.ts`)

Create reusable pagination/sort models used across all entities:

```typescript
// Pagination query parameters
export interface PaginationQuery {
  page: number;
  pageSize: number;
}

// Sort query parameters
export interface SortQuery {
  field?: string;
  descending?: boolean;
}

// Generic paged result
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// Helper to compute pagination metadata
export function getPaginationMetadata(result: PagedResult<any>) {
  const totalPages = Math.ceil(result.totalCount / result.pageSize);
  return {
    totalPages,
    hasNextPage: result.page < totalPages,
    hasPreviousPage: result.page > 1
  };
}
```

### 3. Company Models (`models/company.models.ts`)

Company-specific models (filters, requests, responses):

```typescript
import { PagedResult } from '../../../shared/models/pagination.models';

// Filter parameters (entity-specific)
export interface CompanyFilters {
  name?: string;
}

// Request DTOs
export interface CreateCompanyRequest {
  name: string;
}

export interface UpdateCompanyNameRequest {
  name: string;
}

// Response DTOs
export interface CompanyListItem {
  id: string;
  name: string;
  status: string; // "Active" | "Deleted"
  createdAt: Date; // .NET serializes as ISO string, HttpClient auto-parses to Date
}

export interface CompanyDetailResponse {
  id: string;
  name: string;
  status: string;
  createdAt: Date;
}

// Typed paged result for companies
export type PagedCompaniesResponse = PagedResult<CompanyListItem>;

// Name uniqueness check
export interface CheckNameResponse {
  available: boolean;
  existingCompanyId?: string;
}
```

### 4. Company Service (`services/company.service.ts`)

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginationQuery, SortQuery } from '../../../shared/models/pagination.models';
import {
  CompanyFilters,
  CompanyListItem,
  CompanyDetailResponse,
  PagedCompaniesResponse,
  CreateCompanyRequest,
  UpdateCompanyNameRequest,
  CheckNameResponse
} from '../models/company.models';

@Injectable({
  providedIn: 'root'
})
export class CompanyService {
  private http = inject(HttpClient);

  list(
    filters: CompanyFilters,
    pagination: PaginationQuery,
    sort?: SortQuery
  ): Observable<PagedCompaniesResponse> {
    let params = new HttpParams()
      .set('page', pagination.page.toString())
      .set('pageSize', pagination.pageSize.toString());

    if (filters.name) {
      params = params.set('name', filters.name);
    }

    if (sort?.field) {
      const sortValue = sort.descending ? `${sort.field}:desc` : sort.field;
      params = params.set('sort', sortValue);
    }

    return this.http.get<PagedCompaniesResponse>('/api/companies', { params });
  }

  get(id: string): Observable<CompanyDetailResponse | null> {
    return this.http.get<CompanyDetailResponse | null>(`/api/companies/${id}`);
  }

  create(request: CreateCompanyRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>('/api/companies', request);
  }

  update(id: string, request: UpdateCompanyNameRequest): Observable<void> {
    return this.http.put<void>(`/api/companies/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/companies/${id}`);
  }

  checkName(name: string, excludeId?: string): Observable<CheckNameResponse> {
    let params = new HttpParams().set('name', name);

    if (excludeId) {
      params = params.set('excludeId', excludeId);
    }

    return this.http.get<CheckNameResponse>('/api/companies/check-name', { params });
  }
}
```

### 5. List View Component (`list-view/companies-list.component.ts`)

### 5. List View Component (`list-view/companies-list.component.ts`)

**Architecture Decisions**:
1. **Pagination Directive**: Using `[appTablePagination]` directive approach
   - **Chosen**: Directive for maximum flexibility and customization
   - **Benefit**: Can be applied to any container, developer controls template
   - **Pattern**: Directive manages state/logic, developer provides UI

2. **Loading Strategy**: Use global `LoadingService` vs component-level loading signals?
   - **Global LoadingService**: Consistent spinner in header/layout, single source of truth
   - **Component signals**: More granular control, can show loading state per section
   - **Recommendation**: Use LoadingService for page-level loading (matches existing pattern in app.component.ts)

**Key Features**:
- TanStack Table for headless pagination/sorting/filtering logic
- **Fully functional on mobile** with responsive card layout
- Search by name (500ms debounce)
- Sort by name/createdAt
- Server-side pagination (20 items/page)
- Row actions: Edit, Delete (with confirmation) - **mobile-optimized touch targets**
- Empty state messaging
- Uses LoadingService for consistent loading UX

**Template Structure**:
```html
<!-- Search Bar -->
<div class="flex items-center justify-between mb-4">
  <input
    type="search"
    placeholder="{{ 'companies.name' | transloco }}"
    [(ngModel)]="searchText"
    (ngModelChange)="onSearchChange($event)"
    class="input w-full max-w-md" />

  <button
    appBtn
    (click)="navigateToCreate()">
    <app-icon name="add" />
    {{ 'companies.create' | transloco }}
  </button>
</div>

<!-- Desktop Table (hidden on mobile) -->
@if (!responsive.isMobile() && hasData()) {
  <div class="rounded-md border">
    <table class="w-full">
      <thead>
        <tr class="border-b bg-muted/50">
          <th>{{ 'companies.name' | transloco }}</th>
          <th>{{ 'companies.status' | transloco }}</th>
          <th>{{ 'companies.createdAt' | transloco }}</th>
          <th class="w-[100px]">Actions</th>
        </tr>
      </thead>
      <tbody>
        @for (company of companies(); track company.id) {
          <tr class="border-b hover:bg-muted/50">
            <td>{{ company.name }}</td>
            <td>{{ 'companies.' + company.status.toLowerCase() | transloco }}</td>
            <td>{{ company.createdAt | date:'short' }}</td>
            <td>
              <!-- Actions Menu using @angular/aria -->
              <button appBtn variant="ghost" size="icon" ngMenuTrigger [menu]="actionMenu">
                <app-icon name="more_vert" />
              </button>
              <!-- Menu template... -->
            </td>
          </tr>
        }
      </tbody>
    </table>
  </div>
}

<!-- Mobile Cards (shown on mobile) -->
@if (responsive.isMobile() && hasData()) {
  <div class="space-y-4">
    @for (company of companies(); track company.id) {
      <div class="rounded-lg border bg-card p-4">
        <div class="flex items-start justify-between">
          <div>
            <h3 class="font-semibold">{{ company.name }}</h3>
            <p class="text-sm text-muted-foreground">{{ company.createdAt | date:'short' }}</p>
          </div>
          <!-- Actions button -->
        </div>
      </div>
    }
  </div>
}

<!-- Empty State -->
@if (!loading() && !hasData()) {
  <div class="text-center py-12">
    <p class="text-muted-foreground">{{ 'companies.noResults' | transloco }}</p>
  </div>
}

<!-- Loading State -->
@if (loading()) {
  <div class="text-center py-12">
    <p class="text-muted-foreground">{{ 'companies.loading' | transloco }}</p>
  </div>
}

<!-- Pagination -->
@if (hasData()) {
  <div class="flex items-center justify-between mt-4">
    <span class="text-sm text-muted-foreground">
      Page {{ pagination().page }} of {{ totalPages() }}
    </span>
    <div class="flex gap-2">
      <button
        appBtn
        variant="outline"
        size="sm"
        [disabled]="!hasPreviousPage()"
        (click)="previousPage()">
        Previous
      </button>
      <button
        appBtn
        variant="outline"
        size="sm"
        [disabled]="!hasNextPage()"
        (click)="nextPage()">
        Next
      </button>
    </div>
  </div>
}
```

**Component Logic** (approximate, will be refined):
```typescript
import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { CompanyService } from '../services/company.service';
import { CompanyListItem } from '../models/company.models';
import { PaginationQuery, SortQuery, getPaginationMetadata } from '../../../shared/models/pagination.models';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { LoadingService } from '../../../core/services/loading.service';
import { ResponsiveService } from '../../../core/services/responsive.service';

@Component({
  selector: 'app-companies-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // imports...
  templateUrl: './companies-list.component.html'
})
export class CompaniesListComponent implements OnInit {
  private companyService = inject(CompanyService);
  private router = inject(Router);
  private dialogService = inject(DialogService);
  private toastService = inject(ToastService);
  private loadingService = inject(LoadingService);
  protected responsive = inject(ResponsiveService);

  // State signals
  companies = signal<CompanyListItem[]>([]);
  totalCount = signal(0);
  pagination = signal<PaginationQuery>({ page: 1, pageSize: 20 });
  filters = signal<{ name?: string }>({});
  sort = signal<SortQuery>({ field: 'createdAt', descending: true });

  // Search debouncing
  private searchSubject = new Subject<string>();
  searchText = '';

  // Computed signals
  hasData = computed(() => this.companies().length > 0);

  paginationMeta = computed(() => {
    return getPaginationMetadata({
      items: this.companies(),
      totalCount: this.totalCount(),
      page: this.pagination().page,
      pageSize: this.pagination().pageSize
    });
  });

  ngOnInit(): void {
    this.loadCompanies();

    // Setup search debouncing
    this.searchSubject.pipe(
      debounceTime(500),
      distinctUntilChanged()
    ).subscribe(searchText => {
      this.filters.update(f => ({ ...f, name: searchText || undefined }));
      this.pagination.update(p => ({ ...p, page: 1 })); // Reset to page 1
      this.loadCompanies();
    });
  }

  loadCompanies(): void {
    this.loadingService.show();

    this.companyService.list(
      this.filters(),
      this.pagination(),
      this.sort()
    ).subscribe({
      next: (response) => {
        this.companies.set(response.items);
        this.totalCount.set(response.totalCount);
        this.loadingService.hide();
      },
      error: (error) => {
        this.toastService.error('companies.error.loadFailed');
        this.loadingService.hide();
      }
    });
  }

  onSearchChange(value: string): void {
    this.searchSubject.next(value);
  }

  nextPage(): void {
    if (this.paginationMeta().hasNextPage) {
      this.pagination.update(p => ({ ...p, page: p.page + 1 }));
      this.loadCompanies();
    }
  }

  previousPage(): void {
    if (this.paginationMeta().hasPreviousPage) {
      this.pagination.update(p => ({ ...p, page: p.page - 1 }));
      this.loadCompanies();
    }
  }

  // ... rest of methods (navigateToCreate, editCompany, deleteCompany, sortBy)
}
```

### 6. Details/Edit Form Component (`details/company-details.component.ts`)

**Key Features**:
- Single component for create/edit modes
- Reactive form with validation
- Async validator for name uniqueness (400ms debounce)
- Auto-save disabled (explicit save only per ADR 0011)
- Delete button (edit mode only) with confirmation
- Cancel navigation protection if form dirty
- Uses LoadingService for consistent loading UX
- **Mobile-optimized form layout** with touch-friendly inputs

**Template**:
```html
<div class="max-w-2xl mx-auto">
  <div class="mb-6">
    <h1 class="text-2xl font-bold">
      {{ isCreateMode() ? ('companies.create' | transloco) : ('companies.edit' | transloco) }}
    </h1>
  </div>

  @if (loading()) {
    <p>{{ 'companies.loading' | transloco }}</p>
  } @else {
    <form [formGroup]="form" (ngSubmit)="save()">
      <!-- Name Field -->
      <div class="mb-4">
        <label class="block text-sm font-medium mb-2">
          {{ 'companies.name' | transloco }}
        </label>
        <input
          type="text"
          formControlName="name"
          [placeholder]="'companies.namePlaceholder' | transloco"
          class="input w-full"
          [class.border-destructive]="nameControl.invalid && nameControl.touched" />

        @if (nameControl.invalid && nameControl.touched) {
          <p class="text-sm text-destructive mt-1">
            @if (nameControl.hasError('required')) {
              {{ 'companies.nameRequired' | transloco }}
            }
            @if (nameControl.hasError('nameTaken')) {
              {{ 'companies.nameTaken' | transloco }}
            }
          </p>
        }
      </div>

      <!-- Status (edit mode only) -->
      @if (!isCreateMode() && company()) {
        <div class="mb-4">
          <label class="block text-sm font-medium mb-2">
            {{ 'companies.status' | transloco }}
          </label>
          <p class="text-sm">{{ 'companies.' + company()!.status.toLowerCase() | transloco }}</p>
        </div>

        <div class="mb-4">
          <label class="block text-sm font-medium mb-2">
            {{ 'companies.createdAt' | transloco }}
          </label>
          <p class="text-sm">{{ company()!.createdAt | date:'medium' }}</p>
        </div>
      }

      <!-- Actions -->
      <div class="flex items-center justify-between">
        <div class="flex gap-2">
          <button
            type="submit"
            appBtn
            [disabled]="form.invalid || saving()">
            @if (saving()) {
              {{ 'companies.saving' | transloco }}
            } @else {
              {{ 'companies.saveChanges' | transloco }}
            }
          </button>

          <button
            type="button"
            appBtn
            variant="outline"
            (click)="cancel()">
            {{ 'companies.cancel' | transloco }}
          </button>
        </div>

        @if (!isCreateMode()) {
          <button
            type="button"
            appBtn
            variant="destructive"
            (click)="delete()">
            {{ 'companies.delete' | transloco }}
          </button>
        }
      </div>
    </form>
  }
</div>
```

**Component Logic** (approximate, will be refined):
```typescript
import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, of, map, debounceTime, distinctUntilChanged, switchMap, first } from 'rxjs';
import { CompanyService } from '../services/company.service';
import { CompanyDetailResponse } from '../models/company.models';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { LoadingService } from '../../../core/services/loading.service';

@Component({
  selector: 'app-company-details',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // imports...
  templateUrl: './company-details.component.html'
})
export class CompanyDetailsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private companyService = inject(CompanyService);
  private dialogService = inject(DialogService);
  private toastService = inject(ToastService);
  private loadingService = inject(LoadingService);

  // State
  company = signal<CompanyDetailResponse | null>(null);
  saving = signal(false);
  companyId = signal<string | null>(null);

  // Computed
  isCreateMode = computed(() => this.companyId() === 'new');

  // Form
  form!: FormGroup;

  ngOnInit(): void {
    this.initializeForm();

    this.route.params.subscribe(params => {
      const id = params['id'];
      this.companyId.set(id);

      if (id !== 'new') {
        this.loadCompany(id);
      }
    });
  }

  private loadCompany(id: string): void {
    this.loadingService.show();

    this.companyService.get(id).subscribe({
      next: (company) => {
        if (company) {
          this.company.set(company);
          this.form.patchValue({ name: company.name });
        } else {
          this.toastService.error('companies.error.loadFailed');
          this.router.navigate(['/companies']);
        }
        this.loadingService.hide();
      },
      error: () => {
        this.toastService.error('companies.error.loadFailed');
        this.loadingService.hide();
        this.router.navigate(['/companies']);
      }
    });
  }

  // ... rest of methods
}
```

---

## Headless Table Components Strategy

### Directive-Based Pagination

We're using a directive approach that provides pagination logic while letting developers control the UI:

**Directive Pattern**:
```typescript
// Directive provides: state management, navigation methods, metadata
<div
  appTablePagination
  [totalCount]="totalCount()"
  [pageSize]="pagination().pageSize"
  [currentPage]="pagination().page"
  (pageChange)="onPageChange($event)">

  <!-- Developer controls the template -->
  <button (click)="pagination.previous()" [disabled]="!pagination.hasPrevious()">Previous</button>
  <span>Page {{ pagination.currentPage() }} of {{ pagination.totalPages() }}</span>
  <button (click)="pagination.next()" [disabled]="!pagination.hasNext()">Next</button>
</div>
```

**Benefits**:
- Full control over UI/styling per use case
- Directive handles complex pagination logic
- Computed signals for hasNext/hasPrevious/totalPages
- Can create different layouts (mobile vs desktop) with same logic

### Directives to Create

1. **`[appTablePagination]`** - Pagination logic directive
   - Inputs: totalCount, pageSize, currentPage
   - Outputs: pageChange
   - Provides: pagination context with navigation methods and computed metadata
   - Exposes via template variable: `#pagination="appTablePagination"`

2. **Later**: Column sorting directive, row selection directive (if patterns emerge)

---

### 7. Routing Configuration

Update [app.routes.ts](../../apps/portals/landlord/web/landlord-portal/src/app/app.routes.ts):

```typescript
import { Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { CompaniesListComponent } from './features/companies/list-view/companies-list.component';
import { CompanyDetailsComponent } from './features/companies/details/company-details.component';

export const routes: Routes = [
  // ... existing routes
  {
    path: 'companies',
    canActivate: [AuthGuard],
    children: [
      { path: '', component: CompaniesListComponent },
      { path: 'new', component: CompanyDetailsComponent },
      { path: ':id', component: CompanyDetailsComponent }
    ]
  }
];
```

### 7. Internationalization

Add to [en.json](../../apps/portals/landlord/web/landlord-portal/src/assets/i18n/en.json):

```json
{
  "companies": {
    "title": "Companies",
    "create": "New Company",
    "edit": "Edit Company",
    "delete": "Delete Company",
    "deleteConfirm": "Are you sure you want to delete '{{name}}'? This action cannot be undone.",
    "name": "Company Name",
    "namePlaceholder": "Acme Corporation",
    "nameRequired": "Company name is required",
    "nameTaken": "This company name is already in use",
    "status": "Status",
    "createdAt": "Created",
    "active": "Active",
    "deleted": "Deleted",
    "saveChanges": "Save Changes",
    "saving": "Saving...",
    "cancel": "Cancel",
    "noResults": "No companies found",
    "loading": "Loading companies...",
    "success": {
      "created": "Company created successfully",
      "updated": "Company updated successfully",
      "deleted": "Company deleted successfully"
    },
    "error": {
      "loadFailed": "Failed to load companies",
      "createFailed": "Failed to create company",
      "updateFailed": "Failed to update company",
      "deleteFailed": "Failed to delete company"
    }
  }
}
```

Add to [uk.json](../../apps/portals/landlord/web/landlord-portal/src/assets/i18n/uk.json):

```json
{
  "companies": {
    "title": "Компанії",
    "create": "Нова компанія",
    "edit": "Редагувати компанію",
    "delete": "Видалити компанію",
    "deleteConfirm": "Ви впевнені, що хочете видалити '{{name}}'? Цю дію не можна скасувати.",
    "name": "Назва компанії",
    "namePlaceholder": "ТОВ Компанія",
    "nameRequired": "Назва компанії є обов'язковою",
    "nameTaken": "Ця назва компанії вже використовується",
    "status": "Статус",
    "createdAt": "Створено",
    "active": "Активна",
    "deleted": "Видалена",
    "saveChanges": "Зберегти зміни",
    "saving": "Збереження...",
    "cancel": "Скасувати",
    "noResults": "Компанії не знайдено",
    "loading": "Завантаження компаній...",
    "success": {
      "created": "Компанію успішно створено",
      "updated": "Компанію успішно оновлено",
      "deleted": "Компанію успішно видалено"
    },
    "error": {
      "loadFailed": "Не вдалося завантажити компанії",
      "createFailed": "Не вдалося створити компанію",
      "updateFailed": "Не вдалося оновити компанію",
      "deleteFailed": "Не вдалося видалити компанію"
    }
  }
}
```

---

## Implementation Checklist

### Phase 1: Setup & Shared Infrastructure
- [ ] Install `@tanstack/angular-table`: `npm install @tanstack/angular-table`
- [ ] Create `/shared/models/pagination.models.ts` with PaginationQuery, SortQuery, PagedResult<T>, getPaginationMetadata()
- [ ] Create `/shared/directives/table-pagination/` directory
- [ ] Create `table-pagination.directive.ts` using directive approach
- [ ] Add barrel exports for shared models and directives

### Phase 2: Company Feature Structure
- [ ] Create directory structure: `/features/companies/{list-view,details,models,services}`
- [ ] Create `company.models.ts` with all interfaces (using shared PagedResult)
- [ ] Create `company.service.ts` with CRUD methods (using shared pagination models)
- [ ] Add i18n keys to `en.json` and `uk.json`

### Phase 3: List View
- [ ] Create `companies-list.component.ts`
- [ ] Implement TanStack Table integration
- [ ] Build desktop table layout with sortable columns
- [ ] Build **mobile card layout** (fully functional touch interface)
- [ ] Implement search with debouncing
- [ ] Implement sorting (name, createdAt) with visual indicators
- [ ] Use `[appTablePagination]` directive
- [ ] Add actions menu (@angular/aria ngMenu) with **mobile-optimized touch targets**
- [ ] Implement delete with confirmation dialog
- [ ] Add empty state UI
- [ ] Integrate LoadingService for page-level loading
- [ ] **Test mobile responsiveness**: touch targets, swipe gestures, viewport sizing

### Phase 4: Details/Edit Form
- [ ] Create `company-details.component.ts`
- [ ] Build reactive form with validation
- [ ] Implement async name uniqueness validator
- [ ] Load existing company data (edit mode)
- [ ] Implement save (create/update)
- [ ] Implement delete with confirmation
- [ ] Add cancel navigation
- [ ] Display validation errors inline
- [ ] Integrate LoadingService
- [ ] **Test mobile form layout**: input sizing, keyboard handling, button placement

### Phase 5: Integration
- [ ] Update `app.routes.ts` with companies routes
- [ ] Test navigation from menu to list
- [ ] Test create flow: navigate → validate → save → redirect
- [ ] Test edit flow: load → modify → save → redirect
- [ ] Test delete flow: confirm → delete → refresh list
- [ ] Test search/filter/pagination
- [ ] Test sorting functionality
- [ ] **Test full mobile workflow**: list → create → edit → delete

### Phase 6: Polish & Testing
- [ ] Test all i18n translations (en/uk)
- [ ] Verify accessibility (AXE scan, keyboard nav, screen reader)
- [ ] **Mobile accessibility**: touch target sizes (min 44x44px), gesture support
- [ ] Test error handling (network failures, validation)
- [ ] Test loading states with LoadingService
- [ ] Verify toast notifications appear correctly
- [ ] Test with real backend data
- [ ] Performance check (large datasets, mobile devices)
- [ ] **Cross-device testing**: iOS Safari, Android Chrome, tablets
- [ ] Browser compatibility check (desktop)

---

## Design Decisions

### Architectural Patterns

**Separated Query Models**
- **Why**: Filters, Pagination, Sort are distinct concerns
- **Benefit**: Pagination/Sort models are reusable across all entity lists
- **Pattern**: `service.list(filters, pagination, sort)`

**Generic PagedResult<T>**
- **Why**: All entity lists need same pagination metadata
- **Benefit**: Single source of truth for pagination logic
- **Pattern**: `PagedResult<CompanyListItem>`, `PagedResult<PropertyListItem>`, etc.

**LoadingService vs Component Signals**
- **Decision**: Use LoadingService for page-level loading
- **Why**: Consistent with existing pattern in app.component.ts
- **Benefit**: Single loading spinner in header/layout, no per-component implementation
- **Trade-off**: Less granular control (acceptable for MVP)

**Headless Table Directives**
- **Decision**: Use directive approach for pagination (not component)
- **Why**: Maximum flexibility, developers control UI/layout, works for both mobile/desktop
- **Pattern**: `[appTablePagination]` with template variable access
- **Benefit**: Same logic, different UI per context (list vs mobile cards)

**Date Serialization**
- **Decision**: Use Date type in TypeScript, let .NET/HttpClient handle serialization
- **Why**: .NET serializes to ISO 8601 strings, Angular HttpClient auto-parses to Date objects
- **Benefit**: No manual string→Date conversion needed

### Feature Patterns

**Why TanStack Table?**
- **Headless**: Logic separated from UI, full control over styling
- **Lightweight**: ~14KB gzipped vs 300KB+ for full component libraries
- **Framework Agnostic**: Consistent patterns across Angular/React/Vue
- **Feature Complete**: Pagination, sorting, filtering, column visibility built-in
- **Recommended**: ADR 0012 for complex data tables

**Why Explicit Save (No Auto-Save)?**
- **Single Field**: Company only has name field, auto-save adds complexity without value
- **User Expectation**: Traditional form pattern (save/cancel) is clearer
- **Network Efficiency**: Single save request vs multiple on blur
- **Per ADR 0011**: Auto-save for simple fields, explicit for complex sections

**Why No Column Visibility Toggle?**
- **MVP Scope**: Only 3 columns (name, status, created), all essential
- **Future Enhancement**: Add when columns exceed 6 (e.g., address, contacts, etc.)
- **Mobile**: Responsive card layout handles visibility naturally

**Why Server-Side Pagination?**
- **Scalability**: Thousands of companies possible in production
- **Performance**: Load only 20 items per request
- **Future Features**: Enables backend filtering/search optimizations

**Why Async Name Validator?**
- **Real-Time Feedback**: User knows immediately if name is taken
- **UX**: Avoids form submission errors
- **Debounced**: 400ms delay reduces API calls while typing

### Mobile-First Requirements

**Critical Mobile Features**:
- Touch targets minimum 44x44px (iOS/Android guidelines)
- Responsive card layout for tables on small screens
- Mobile-optimized forms with appropriate input types
- Touch-friendly action menus (no hover states)
- Swipe gestures for common actions (future enhancement)
- Proper viewport meta tags and CSS touch-action
- Test on actual devices, not just browser DevTools

---

## Future Enhancements

### Phase 2 (Post-MVP)
- [ ] **Bulk Operations**: Select multiple companies, bulk delete
- [ ] **Advanced Filters**: Status filter, date range picker
- [ ] **Column Sorting**: Multi-column sort (Shift+Click)
- [ ] **Export**: CSV/Excel export of filtered list
- [ ] **Column Visibility**: Toggle optional columns (when > 6 columns exist)

### Phase 3 (Complex Workflows)
- [ ] **Company Details Tabs**: Contacts, Addresses, Properties (when added)
- [ ] **Audit Log**: Track company changes over time
- [ ] **Real-Time Updates**: SignalR for concurrent user changes
- [ ] **Advanced Search**: Full-text search across all fields

---

## Open Discussions

Before implementation, we need to finalize:

1. **Loading Strategy Confirmation**
   - [ ] Global LoadingService (current recommendation)
   - [ ] Component-level loading signals
   - **Recommendation**: LoadingService to match existing pattern

2. **Mobile Touch Targets**
   - Confirm minimum sizes (44x44px standard)
   - Action menu design on mobile (sheet vs popover)
   - Consider adding swipe-to-delete gesture (future)

---

## Next Steps

1. **Review and approve** this implementation plan
2. **Finalize decisions** on open discussions above
3. **Start Phase 1**: Create shared infrastructure (pagination models, table components)
4. **Iterate phase-by-phase** with testing at each step
5. **Refine code samples** during actual implementation

**Ready to start building!** 🚀

---

## References

- [ADR 0011: Entity Management UI Patterns](../decisions/0011-entity-management-ui-patterns.md)
- [ADR 0012: Headless UI Component Strategy](../decisions/0012-headless-ui-component-strategy.md)
- [Backend Company Service](../../apps/services/ProperTea.Company)
- [BFF Company Endpoints](../../apps/portals/landlord/bff/ProperTea.Landlord.Bff/Companies)
- [LoadingService Implementation](../../apps/portals/landlord/web/landlord-portal/src/app/core/services/loading.service.ts)
