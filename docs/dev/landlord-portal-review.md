# Landlord Portal - Code Review Fixes

Reference document for fixing issues identified in the landlord-portal Angular project.

- **Project root**: `apps/portals/landlord/web/landlord-portal/`
- **Angular version**: 21 (v20+ defaults `standalone: true` -- must NOT be set explicitly)
- **Change detection**: Zoneless (`provideZonelessChangeDetection()`)
- **Styling**: Tailwind 4, CVA for variant management
- **i18n**: Transloco
- **Headless-first**: Simple components are directives (e.g., `[appBtn]`), complex interactive ones use `@angular/aria`

All file paths below are relative to `src/` inside the project root unless stated otherwise.

---

## 1. Bugs (fix immediately)

### 1.1 BadgeDirective uses @HostBinding with class-stacking bug

**File**: `shared/components/badge/badge.directive.ts`

**Problem**: Uses `@HostBinding('class')` with a getter that reads `el.className`, which includes CVA classes from the previous render. This causes infinite class accumulation on each change detection cycle. Also violates project rule: "Do NOT use `@HostBinding`."

**Fix**: Refactor to match the `ButtonDirective` pattern:

1. Remove the `@HostBinding('class')` getter.
2. Remove the `ElementRef` injection.
3. Add a `computed()` signal that wraps the CVA call.
4. Use the `host` object in the `@Directive` decorator:

```typescript
host: {
  '[class]': 'computedClasses()'
}
```

The `computedClasses` signal should call the CVA function with the current input values, returning only the classes CVA produces (no reading from the DOM).

---

### 1.2 `standalone: true` set explicitly on two components

**Files**:
- `shared/components/theme-toggle/theme-toggle.component.ts`
- `shared/components/language-selector/language-selector.component.ts`

**Problem**: Angular 20+ defaults to standalone. Setting it explicitly is forbidden by project guidelines.

**Fix**: Remove `standalone: true` from both `@Component` decorators.

---

### 1.3 Docs page uses wrong directive selector `appButton` instead of `appBtn`

**File**: `app/features/docs/docs.page.ts` (around line 96 in the sidebar button area)

**Problem**: The sidebar nav buttons use `appButton` but the actual directive selector is `[appBtn]`. Those buttons receive zero directive styling.

**Fix**: Change all `appButton` occurrences to `appBtn` in that template. Verify that `ButtonDirective` is in the `imports` array (it already is).

---

### 1.4 HealthService.startMonitoring() called twice -- duplicate interval subscriptions

**Files**:
- `app/layout/layout.component.ts` (`ngOnInit` calls `startMonitoring()`)
- `app/layout/footer/footer.component.ts` (`ngOnInit` calls `startMonitoring(30000)`)

**Problem**: Two independent `interval()` subscriptions polling `/health`, doubling traffic. Neither is ever unsubscribed.

**Fix**:
1. Remove the `startMonitoring()` call from `FooterComponent.ngOnInit()`.
2. Keep the call only in `AppLayoutComponent`.
3. Refactor `HealthService` to be idempotent -- guard against multiple `startMonitoring()` calls (e.g., check a boolean flag before creating the interval).
4. Add a `stopMonitoring()` method to `HealthService` (see 1.6).
5. Call `stopMonitoring()` from `AppLayoutComponent.ngOnDestroy()` (see 1.9).

---

### 1.5 UserPreferencesService.loadPreferences() called from two places

**Files**:
- `app/layout/layout.component.ts` (`ngOnInit`)
- `app/features/docs/docs.page.ts` (`ngOnInit`)

**Problem**: Double-loading preferences. The DocsPage call serves the public page (outside layout), so both calls are valid in their respective contexts.

**Fix**: Keep both calls, but make `loadPreferences()` idempotent. Add a `loaded` flag inside `UserPreferencesService` so the HTTP request only fires once. Subsequent calls should return immediately (or return the cached observable).

---

### 1.6 HealthService interval() never unsubscribed -- memory leak

**File**: `app/core/services/health.service.ts`

**Problem**: `startMonitoring()` subscribes to an infinite `interval()` observable and stores no reference to unsubscribe.

**Fix**:
1. Store the `Subscription` in a private field.
2. Add a `stopMonitoring()` method that calls `subscription.unsubscribe()` and resets the monitoring flag.
3. Guard `startMonitoring()` to be idempotent -- if already monitoring, return early.

```typescript
private subscription: Subscription | null = null;
private monitoring = false;

startMonitoring(intervalMs = 60000): void {
  if (this.monitoring) return;
  this.monitoring = true;
  this.subscription = interval(intervalMs).pipe(
    // ... existing pipe logic
  ).subscribe();
}

stopMonitoring(): void {
  this.subscription?.unsubscribe();
  this.subscription = null;
  this.monitoring = false;
}
```

---

### 1.7 Dashboard template has broken HTML nesting

**File**: `app/features/dashboard/dashboard-home.page.ts` (the "Recent Activity" section in the template)

**Problem**: The closing `</div>` tags are misaligned. The first activity item's wrapper div is not properly closed before the second item begins, causing rendering issues.

**Fix**: Re-indent and fix the nesting. Each activity item must follow this structure:

```html
<div class="flex items-center gap-4">
  <div class="h-2 w-2 rounded-full bg-green-500"></div>
  <div class="flex-1">
    <p>...</p>
    <p>...</p>
  </div>
</div>
```

Ensure every opening `<div>` has a corresponding `</div>` at the correct nesting level. Do not alter styling classes -- only fix the nesting.

---

### 1.8 App component missing lifecycle interface declarations

**File**: `app/app.component.ts`

**Problem**: Defines `ngOnInit` and `ngOnDestroy` methods but doesn't declare `implements OnInit, OnDestroy`.

**Fix**: Add `implements OnInit, OnDestroy` to the class declaration. Import `OnInit` and `OnDestroy` from `@angular/core`.

---

### 1.9 AppLayoutComponent declares OnInit but not OnDestroy, and has no cleanup

**File**: `app/layout/layout.component.ts`

**Problem**: Starts health monitoring in `ngOnInit` but never stops it. Does not implement `OnDestroy`.

**Fix**:
1. Add `OnDestroy` to the `implements` clause.
2. Import `OnDestroy` from `@angular/core`.
3. Add an `ngOnDestroy()` method that calls `this.healthService.stopMonitoring()`.

---

### 1.10 NavigationComponent.expandedMenus is a mutable Set -- won't trigger CD in zoneless mode

**File**: `app/layout/navigation/navigation.component.ts`

**Problem**: `expandedMenus = new Set<string>()` is a plain mutable object. In zoneless Angular, mutating a `Set` does NOT trigger change detection. Expanding/collapsing submenus will silently fail to render.

**Fix**: Convert to a signal-based approach:

```typescript
expandedMenus = signal(new Set<string>());

toggleSubmenu(label: string): void {
  this.expandedMenus.update(set => {
    const next = new Set(set);
    if (next.has(label)) {
      next.delete(label);
    } else {
      next.add(label);
    }
    return next;
  });
}
```

Update all template references from `expandedMenus.has(...)` to `expandedMenus().has(...)`.

---

## 2. Inconsistencies (address in same pass)

### 2.1 Two shared component locations

**Problem**: Both `src/shared/components/` and `src/app/shared/components/` exist. The latter is empty. This is confusing.

**Fix**: Delete the empty `src/app/shared/` directory entirely (including the `components/` subfolder). All shared components live in `src/shared/components/`.

---

### 2.2 Import path style inconsistency (barrel vs full path)

**Problem**: Some files import from barrel index files (`'../shared/components/button'`), others from full paths (`'../shared/components/button/button.directive'`).

**Fix**: Use barrel paths wherever an `index.ts` exists. For components that lack barrel files, create them (see 2.3).

---

### 2.3 Missing barrel index.ts files

**Directories missing barrels**:
- `shared/components/spinner/`
- `shared/components/theme-toggle/`
- `shared/components/language-selector/`

**Fix**: Create an `index.ts` in each with the appropriate export:

```typescript
// spinner/index.ts
export * from './spinner.component';

// theme-toggle/index.ts
export * from './theme-toggle.component';

// language-selector/index.ts
export * from './language-selector.component';
```

---

### 2.4 LogoComponent lives in layout/ but is used by public pages

**File**: `app/layout/logo/`

**Problem**: `LogoComponent` is imported by landing, docs, and register pages that live outside the layout. It is a pure presentational component and doesn't belong in `layout/`.

**Fix**:
1. Move `LogoComponent` to `shared/components/logo/`.
2. Create a barrel `index.ts` exporting the component.
3. Update all imports throughout the project to point to the new location.

---

### 2.5 ThemeToggleComponent and LanguageSelectorComponent depend on core services (inverted dependency)

**Files**:
- `shared/components/theme-toggle/theme-toggle.component.ts`
- `shared/components/language-selector/language-selector.component.ts`

**Problem**: These shared-library components import from `app/core/services/`, creating an upward dependency (`shared` -> `core`). Shared components should not depend on application-level services.

**Fix**: Move both components to `app/core/components/` since they are tightly coupled to `UserPreferencesService` and are app-specific.
1. Move `theme-toggle/` to `app/core/components/theme-toggle/`.
2. Move `language-selector/` to `app/core/components/language-selector/`.
3. Create barrel `index.ts` files in the new locations.
4. Update all imports throughout the project.

Note: The public-page versions (marketing/landing) and the ERP header dropdown version serve different purposes and should remain separate implementations.

---

### 2.6 changeDetection: OnPush missing from all components

**Problem**: Project guidelines mandate `changeDetection: ChangeDetectionStrategy.OnPush` on every `@Component` decorator. None of the components currently set it.

**Fix**: Add `changeDetection: ChangeDetectionStrategy.OnPush` to every `@Component` decorator. Import `ChangeDetectionStrategy` from `@angular/core`.

**Affected components** (every component in the project):

| Component | File (post-refactor paths) |
|---|---|
| `App` | `app/app.component.ts` |
| `AppLayoutComponent` | `app/layout/layout.component.ts` |
| `HeaderComponent` | `app/layout/header/header.component.ts` |
| `FooterComponent` | `app/layout/footer/footer.component.ts` |
| `NavigationComponent` | `app/layout/navigation/navigation.component.ts` |
| `BreadcrumbComponent` | `app/layout/breadcrumb/breadcrumb.component.ts` |
| `LogoComponent` | `shared/components/logo/logo.component.ts` |
| `SpinnerComponent` | `shared/components/spinner/spinner.component.ts` |
| `ThemeToggleComponent` | `app/core/components/theme-toggle/theme-toggle.component.ts` |
| `LanguageSelectorComponent` | `app/core/components/language-selector/language-selector.component.ts` |
| `IconComponent` | `shared/components/icon/icon.component.ts` |
| `ConfirmDialogComponent` | `shared/components/confirm-dialog/confirm-dialog.component.ts` |
| `LandingPage` | `app/features/landing/landing.page.ts` |
| `DocsPage` | `app/features/docs/docs.page.ts` |
| `DashboardHomePage` | `app/features/dashboard/dashboard-home.page.ts` |
| `RegisterOrganizationPage` | `app/features/organizations/pages/register/register-organization.page.ts` |

---

### 2.7 TranslocoModule vs TranslocoPipe import inconsistency

**Problem**: Some components import `TranslocoModule`, others import `TranslocoPipe`. `TranslocoPipe` is preferred for tree-shaking.

**Fix**: Replace `TranslocoModule` with `TranslocoPipe` in:

- `app/features/landing/landing.page.ts`
- `app/features/docs/docs.page.ts` -- Note: the docs page doesn't currently use transloco in its template (content is hardcoded). Either import `TranslocoPipe` for future use or remove the transloco import entirely. If removing, also remove unused `TranslocoModule` from `imports`.
- `app/features/dashboard/dashboard-home.page.ts`
- `app/features/organizations/pages/register/register-organization.page.ts`

---

## 3. Structural Issues

### 3.1 Phantom Spartan tsconfig paths

**File**: `tsconfig.json` (project root)

**Problem**: Approximately 50 `@spartan-ng/helm/*` path aliases point to `./libs/ui/*/src/index.ts` but the `libs/` directory does not exist. These are leftover from scaffolding and cause misleading IDE suggestions.

**Fix**: Remove ALL `@spartan-ng/helm/*` entries from `compilerOptions.paths` in `tsconfig.json`. After cleanup, the `paths` object should be empty or removed entirely.

---

### 3.2 ConfirmDialogComponent should move to shared library

**File**: `app/core/components/confirm-dialog/`

**Problem**: `ConfirmDialogComponent` is a generic reusable UI primitive (dialog with confirm/cancel buttons). It does not belong in `core/`.

**Fix**:
1. Move to `shared/components/confirm-dialog/`.
2. Create a barrel `index.ts`.
3. Update imports in `DialogService` and anywhere else it's referenced.

---

### 3.3 DialogService uses manual DOM manipulation without a11y

**File**: `app/core/services/dialog.service.ts`

**Problem**: Uses `createComponent()` + `document.body.appendChild()` to render dialogs. No focus trapping, no Escape key handling, no scroll lock.

**Fix (minimal, for now)**:
1. Add `aria-modal="true"` and `role="dialog"` to the confirm dialog template.
2. Add an Escape key handler to dismiss the dialog.

Full refactor to `@angular/cdk` Dialog or Overlay is a follow-up task.

---

### 3.4 Stale unit test

**File**: `app/app.spec.ts`

**Problem**:
- Imports from `'./app'` but the file is `app.component.ts`.
- Test asserts that `<h1>` contains "Hello, landlord-portal" which doesn't match the actual template.

**Fix**:
1. Update import to `'./app.component'`.
2. Remove or rewrite the `should render title` test. The template is conditional on auth state, so a simple title assertion is meaningless. Removing the test is acceptable.

---

### 3.5 Interceptors documentation references need removal

**File**: `app/core/README.md`

**Problem**: References an `interceptors/` directory with correlation-id and error interceptors that don't exist and won't be built for now.

**Fix**:
1. Remove the interceptors bullet from the README.
2. Delete the empty `app/core/interceptors/` directory if it exists.

---

### 3.6 Empty models directory in core

**File**: `app/core/models/` (empty directory)

**Problem**: The README references `error.models.ts` but the directory is empty.

**Fix**: Delete the empty `app/core/models/` directory. Remove the reference from the README.

---

### 3.7 Missing wildcard/404 route

**File**: `app/app.routes.ts`

**Problem**: No catch-all route for unmatched URLs. Navigating to an unknown path shows a blank page.

**Fix**: Add a wildcard route at the end of the routes array:

```typescript
{ path: '**', redirectTo: '' }
```

---

## 4. Improvements

### 4.1 SpinnerComponent.spinnerClasses should be computed()

**File**: `shared/components/spinner/spinner.component.ts`

**Problem**: `spinnerClasses` is an arrow function property, not a `computed()` signal. It reads `size()` but won't be tracked as a reactive dependency.

**Fix**: Change to a `computed()` signal:

```typescript
protected spinnerClasses = computed(() => {
  // ... existing logic using this.size()
});
```

Import `computed` from `@angular/core`.

---

### 4.2 LogoComponent.iconSize() and textClass() should be computed()

**File**: `shared/components/logo/logo.component.ts` (post-move path)

**Problem**: `iconSize()` and `textClass()` are methods that read signal `size()` but are not `computed()` signals. They are called in the template on every render without reactive tracking.

**Fix**: Convert both to `computed()` signals:

```typescript
protected iconSize = computed(() => {
  // ... existing logic using this.size()
});

protected textClass = computed(() => {
  // ... existing logic using this.size()
});
```

Update template references if they used method-call syntax (e.g., `iconSize()` stays the same since `computed()` is also called with `()`).

---

### 4.3 Toast messages in register page not internationalized

**File**: `app/features/organizations/pages/register/register-organization.page.ts`

**Problem**: `toastService.error('Failed to create organization...')` and `toastService.success('Organization created successfully!')` use hardcoded English strings.

**Fix**:
1. Inject `TranslocoService` into the component.
2. Replace hardcoded strings with translated keys:

```typescript
this.toastService.error(this.translocoService.translate('register.error.createFailed'));
this.toastService.success(this.translocoService.translate('register.success.created'));
```

3. Add translation keys to both locale files:

**`assets/i18n/en.json`**:
```json
"register": {
  "error": {
    "createFailed": "Failed to create organization. Please try again."
  },
  "success": {
    "created": "Organization created successfully!"
  }
}
```

**`assets/i18n/uk.json`**:
```json
"register": {
  "error": {
    "createFailed": "Не вдалося створити організацію. Спробуйте ще раз."
  },
  "success": {
    "created": "Організацію успішно створено!"
  }
}
```

---

### 4.4 Breadcrumb "Home" is hardcoded English

**File**: `app/layout/breadcrumb/breadcrumb.component.ts`

**Problem**: The template contains a hardcoded string `Home`.

**Fix**:
1. Import `TranslocoPipe` from `@jsverse/transloco`.
2. Add `TranslocoPipe` to the component's `imports` array.
3. Replace `Home` in the template with `{{ 'nav.home' | transloco }}`.
4. Add translation keys:

**`assets/i18n/en.json`** (inside the `nav` section):
```json
"home": "Home"
```

**`assets/i18n/uk.json`** (inside the `nav` section):
```json
"home": "Головна"
```

---

### 4.5 AppLayoutComponent.openProfile() has hardcoded localhost URL

**File**: `app/layout/layout.component.ts`

**Problem**: `const idpUrl = 'http://localhost:9080'` is hardcoded.

**Fix (minimal)**: Extract to a clearly marked constant at the top of the file with a TODO comment:

```typescript
// TODO: Move to environment config or read from BFF config endpoint
const IDP_BASE_URL = 'http://localhost:9080';
```

Then reference `IDP_BASE_URL` in `openProfile()`. A proper fix (config service, BFF proxy, or environment injection) is a follow-up task.

---

### 4.6 OrganizationService.checkName() should use HttpParams

**File**: `app/features/organizations/services/organization.service.ts`

**Problem**: Manually constructs query string using string interpolation, which is fragile and doesn't encode special characters.

**Fix**: Use Angular's `HttpParams`:

```typescript
import { HttpParams } from '@angular/common/http';

checkName(name: string): Observable<CheckNameResponse> {
  const params = new HttpParams().set('name', name);
  return this.http.get<CheckNameResponse>('/api/organizations/check-name', { params });
}
```

---

## 5. Do Not Change

These items were reviewed and are intentional decisions. Do not modify them.

### Docs page hardcoded content

**File**: `app/features/docs/docs.page.ts`

The documentation content is hardcoded English. This is intentional. The documentation system will be rebuilt from scratch with `.md` file rendering. Do not internationalize or refactor.

### LanguageSelectorComponent kept separate from ERP dropdown

The public-page version of the language selector (used on landing, docs, register pages) is a standalone "pretty" implementation for marketing display. The ERP header dropdown version is a functional variant. They serve different purposes and should remain as separate implementations.

### No HTTP interceptors

HTTP interceptors were intentionally dropped from the project for now. Only the stale documentation references need to be cleaned up (see item 3.5).

---

## Suggested execution order

1. **Structural moves first** (2.4, 2.5, 3.2) -- move components to their correct locations.
2. **Create missing barrel files** (2.3) and fix import paths (2.2).
3. **Fix bugs** (1.1 through 1.10) -- these are correctness issues.
4. **Apply blanket fixes** (2.6 OnPush, 2.7 TranslocoPipe, 1.2 standalone removal).
5. **Structural cleanup** (3.1, 3.4, 3.5, 3.6, 3.7, 2.1).
6. **Improvements** (4.1 through 4.6).
7. **Verify** -- run `ng build` and `ng test` after all changes.
