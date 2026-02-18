# Phase 3: Spartan Migration Fixes and Adoption

Phase 2 migrated Icons, Toast, Dialog, Select, Menus, Tabs, and Accordion to Spartan and removed `@angular/material` + `@angular/aria`. The build passes but the application has visual and functional issues. This phase fixes all issues, adopts remaining Spartan components, consolidates form fields, and removes dead code.

Read this document fully before beginning. Each tier is ordered by dependency. Complete all steps within a tier before moving to the next. Run `ng build` after each step to verify.

All paths are relative to `apps/portals/landlord/web/landlord-portal/` unless qualified.

---

## Current State

### Generated Helms (22, in `src/shared/components/ui/`)

`accordion`, `alert-dialog`, `autocomplete`, `badge`, `button`, `dialog`, `dropdown-menu`, `form-field`, `icon`, `input`, `input-group`, `label`, `popover`, `select`, `separator`, `sheet`, `sonner`, `spinner`, `tabs`, `textarea`, `tooltip`, `utils`

### tsconfig Path Aliases (in `tsconfig.json`)

All 22 helms have `@spartan-ng/helm/{name}` path aliases pointing to `./src/shared/components/ui/{name}/src/index.ts`. New helms generated in Tier 3 need aliases added.

---

## Tier 1: Critical Fixes

These fix broken functionality visible to users.

### 1.1 Complete ICON_MAP coverage

**File**: `src/shared/components/icon/icon.component.ts`

The current ICON_MAP has 22 entries. 15 icon names used in templates have no mapping and render blank.

**Add these imports** (after the existing lucide imports, L3-29):

```typescript
import {
  // ... existing imports ...
  lucideArrowDown,
  lucideArrowUp,
  lucideArrowUpDown,
  lucideBuilding,
  lucideCircleAlert,
  lucideCircleCheck,
  lucideDownload,
  lucideHourglass,
  lucideInbox,
  lucideLandmark,
  lucideNetwork,
  lucidePencil,
  lucideSave,
  lucideTrash2,
  lucideLayoutDashboard,
} from '@ng-icons/lucide';
```

**Add these entries to the ICON_MAP** (after the existing entries, L33-55):

```typescript
apartment: 'lucideBuilding',
arrow_downward: 'lucideArrowDown',
arrow_upward: 'lucideArrowUp',
check_circle: 'lucideCircleCheck',
corporate_fare: 'lucideNetwork',
dashboard: 'lucideLayoutDashboard',
delete: 'lucideTrash2',
domain: 'lucideLandmark',
download: 'lucideDownload',
edit: 'lucidePencil',
error: 'lucideCircleAlert',
hourglass_empty: 'lucideHourglass',
inbox: 'lucideInbox',
save: 'lucideSave',
unfold_more: 'lucideArrowUpDown',
```

**Add to `provideIcons`** (L63-89): Register all 15 new icons in the object passed to `provideIcons({...})`.

**Also**: In `src/app/features/properties/details/property-details.component.html`, the accordion trigger (approximately L143) has an inline SVG chevron. Replace it with `<app-icon name="expand_more" [size]="20" [class.rotate-180]="buildingsAccordionOpen()" />` (the `expand_more` mapping already exists).

**Also**: In `src/app/features/properties/details/property-details.component.html`, approximately L136, there is a broken `appButton` attribute (no directive exists for this selector). Replace:

```html
<!-- BEFORE -->
<button type="button" appButton variant="secondary" size="sm" ...>

<!-- AFTER -->
<button type="button" hlmBtn variant="secondary" size="sm" ...>
```

Ensure `HlmButton` is in the component's `imports` array in `property-details.component.ts`.

**Verify**: `ng build` passes. All icons render on navigation sidebar, sort buttons, row action menus, empty states. The accordion "New Building" button is styled correctly.

---

### 1.2 Fix accordion runtime error

The user reports `TypeError: error loading dynamically imported module: .../@spartan-ng_brain_accordion.js`.

**Steps**:

1. Delete the Vite cache:
   ```bash
   rm -rf node_modules/.vite
   ```
2. Restart the dev server (`ng serve`)
3. If the error persists, verify `@spartan-ng/brain` in `package.json` and reinstall:
   ```bash
   npm install @spartan-ng/brain@alpha.637
   ```
4. Verify `@spartan-ng/brain/accordion` resolves. Check `node_modules/@spartan-ng/brain/accordion/` exists and has an `index.js`.

**Verify**: Navigate to Property Details view. Expand the Buildings accordion. No console error.

---

### 1.3 Fix autocomplete placeholder translation

**File**: `src/shared/components/autocomplete/autocomplete.component.ts`

The `[placeholder]="placeholder()"` binding passes the raw translation key (e.g., `'common.search'`) to the brain component without translating it.

**Steps**:

1. Add `TranslocoService` injection:
   ```typescript
   import { TranslocoService } from '@jsverse/transloco';
   // ...
   private readonly translocoService = inject(TranslocoService);
   ```

2. Add a computed signal for translated placeholder:
   ```typescript
   protected readonly translatedPlaceholder = computed(() => {
     const key = this.placeholder();
     return key ? this.translocoService.translate(key) : '';
   });
   ```

3. In `autocomplete.component.html`, change L10:
   ```html
   <!-- BEFORE -->
   [placeholder]="placeholder()"

   <!-- AFTER -->
   [placeholder]="translatedPlaceholder()"
   ```

**Verify**: The autocomplete in create-building-drawer shows "Search..." (not "common.search") as placeholder text.

---

### 1.4 Add missing translations

**Files**: `src/assets/i18n/en.json`, `src/assets/i18n/uk.json`

The audit log for organizations generates event keys dynamically. The key `organizations.events.linked` has no translation entry.

In `en.json`, under `organizations.events` (after the existing entries around L354-359), add:

```json
"linked": "Organization Linked"
```

In `uk.json`, under `organizations.events` (after the existing entries around L185-190), add:

```json
"linked": "Організацію зв'язано"
```

**Verify**: Navigate to Organization Details > History tab. The "Linked" event shows translated text, not a raw key.

---

### 1.5 Fix create button SPA navigation

**File**: `src/shared/components/entity-list-view/entity-list-view.component.html`

The create button (approximately L120-121) uses `[href]` which causes a full page reload.

```html
<!-- BEFORE (L120-121) -->
<a
  [href]="createRoute()"
  hlmBtn
  ...>

<!-- AFTER -->
<a
  [routerLink]="createRoute()"
  hlmBtn
  ...>
```

**File**: `src/shared/components/entity-list-view/entity-list-view.component.ts`

Add `RouterLink` to imports:

```typescript
import { RouterLink } from '@angular/router';
// ... in the @Component imports array:
imports: [
  // ... existing imports
  RouterLink,
],
```

**Verify**: Clicking "New Property" etc. navigates without full page reload. The URL changes via Angular router.

---

## Tier 2: Component Migrations

### 2.1 Migrate entity-details-view from CDK menu to HlmDropdownMenu

**Files**:
- `src/shared/components/entity-details-view/entity-details-view.component.ts`
- `src/shared/components/entity-details-view/entity-details-view.component.html`
- `src/shared/components/entity-details-view/entity-details-view.component.css`

This component still uses `cdkMenuTriggerFor`, `cdkMenu`, `cdkMenuItem` from `@angular/cdk/overlay` and has 78 lines of hand-written menu CSS.

**TS changes**:

```typescript
// BEFORE
import { OverlayModule } from '@angular/cdk/overlay';
// ...
imports: [
  OverlayModule,
  TranslocoPipe,
  HlmButton,
  IconComponent,
],

// AFTER
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';
// ...
imports: [
  HlmDropdownMenuImports,
  TranslocoPipe,
  HlmButton,
  IconComponent,
],
```

Remove the `actionsMenuOpen` signal (HlmDropdownMenu manages its own open state).

Remove `styleUrl: './entity-details-view.component.css'` from the component decorator.

**HTML changes** - Replace the CDK toolbar directive:

```html
<!-- BEFORE -->
<div cdkToolbar class="flex items-center justify-between gap-4">

<!-- AFTER -->
<div role="toolbar" class="flex items-center justify-between gap-4">
```

Replace mobile menu trigger (approximately L62):

```html
<!-- BEFORE -->
<button ... cdkMenuTriggerFor="mobileActionsMenu" ...>

<!-- AFTER -->
<button ... [hlmDropdownMenuTrigger]="mobileActionsMenu" ...>
```

Replace desktop overflow menu trigger (approximately L89):

```html
<!-- BEFORE -->
<button ... cdkMenuTriggerFor="actionsMenu" ...>

<!-- AFTER -->
<button ... [hlmDropdownMenuTrigger]="actionsMenu" ...>
```

Replace both menu `<ng-template>` blocks (secondary actions menu and mobile actions menu). The pattern for each:

```html
<!-- BEFORE (Secondary Actions Menu, ~L113) -->
<ng-template #actionsMenu>
  <div cdkMenu class="menu-content min-w-50">
    @for (action of config().secondaryActions; track trackByLabel($index, action)) {
      @if (action.separatorBefore) {
        <div class="menu-separator"></div>
      }
      <button
        type="button"
        cdkMenuItem
        [disabled]="action.disabled"
        [attr.data-variant]="action.variant || 'default'"
        (cdkMenuItemTriggered)="handleAction(action)">
        @if (action.icon) {
          <app-icon [name]="action.icon" [size]="16" />
        }
        <span>{{ action.label | transloco }}</span>
      </button>
    }
  </div>
</ng-template>

<!-- AFTER -->
<ng-template #actionsMenu>
  <div hlmDropdownMenu class="min-w-50">
    @for (action of config().secondaryActions; track trackByLabel($index, action)) {
      @if (action.separatorBefore) {
        <div hlmDropdownMenuSeparator></div>
      }
      <button
        hlmDropdownMenuItem
        [variant]="action.variant === 'destructive' ? 'destructive' : 'default'"
        [disabled]="action.disabled"
        (click)="handleAction(action)">
        @if (action.icon) {
          <app-icon [name]="action.icon" [size]="16" />
        }
        <span>{{ action.label | transloco }}</span>
      </button>
    }
  </div>
</ng-template>
```

Apply the same pattern to the mobile actions menu template (`#mobileActionsMenu`), which combines primary + secondary actions.

**Delete**: `src/shared/components/entity-details-view/entity-details-view.component.css` (all 78 lines are CDK menu styling now handled by HlmDropdownMenu).

**Verify**: `ng build` passes. Navigate to any detail view (property, building, company). Click the overflow menu (desktop) or actions button (mobile). Menu opens with correct styling, destructive items show red text.

---

### 2.2 Migrate ConfirmDialog from Dialog to AlertDialog

**File**: `src/shared/components/confirm-dialog/confirm-dialog.component.ts`

Current implementation uses `BrnDialogRef` + `injectBrnDialogContext` + `HlmDialog*` components. AlertDialog is more semantically correct for confirmation flows (prevents accidental dismissal, has built-in action/cancel semantics).

```typescript
// BEFORE
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog';
import { HlmButton } from '@spartan-ng/helm/button';
import {
  HlmDialogDescription,
  HlmDialogFooter,
  HlmDialogHeader,
  HlmDialogTitle,
} from '@spartan-ng/helm/dialog';

// AFTER
import { BrnAlertDialogRef, injectBrnAlertDialogContext } from '@spartan-ng/brain/alert-dialog';
import { HlmButton } from '@spartan-ng/helm/button';
import {
  HlmAlertDialogDescription,
  HlmAlertDialogFooter,
  HlmAlertDialogHeader,
  HlmAlertDialogTitle,
} from '@spartan-ng/helm/alert-dialog';
```

Update imports array:

```typescript
// BEFORE
imports: [
  HlmButton,
  TranslocoPipe,
  HlmDialogHeader,
  HlmDialogTitle,
  HlmDialogDescription,
  HlmDialogFooter,
],

// AFTER
imports: [
  HlmButton,
  TranslocoPipe,
  HlmAlertDialogHeader,
  HlmAlertDialogTitle,
  HlmAlertDialogDescription,
  HlmAlertDialogFooter,
],
```

Update template:

```html
<!-- BEFORE -->
<hlm-dialog-header>
  <h3 hlmDialogTitle>{{ data.title }}</h3>
  <p hlmDialogDescription>{{ data.description }}</p>
</hlm-dialog-header>
<hlm-dialog-footer class="mt-4 flex justify-end gap-2">
  ...
</hlm-dialog-footer>

<!-- AFTER -->
<hlm-alert-dialog-header>
  <h3 hlmAlertDialogTitle>{{ data.title }}</h3>
  <p hlmAlertDialogDescription>{{ data.description }}</p>
</hlm-alert-dialog-header>
<hlm-alert-dialog-footer class="mt-4 flex justify-end gap-2">
  ...
</hlm-alert-dialog-footer>
```

Update context injection and ref:

```typescript
// BEFORE
protected readonly data = injectBrnDialogContext<ConfirmDialogData>();
private readonly dialogRef = inject(BrnDialogRef<boolean>);

// AFTER
protected readonly data = injectBrnAlertDialogContext<ConfirmDialogData>();
private readonly dialogRef = inject(BrnAlertDialogRef<boolean>);
```

**File**: `src/app/core/services/dialog.service.ts`

```typescript
// BEFORE
import { HlmDialogService } from '@spartan-ng/helm/dialog';
// ...
private readonly dialogService = inject(HlmDialogService);
confirm(data: ConfirmDialogData): Observable<boolean> {
  const ref = this.dialogService.open(ConfirmDialogComponent, {
    context: data,
  });
  return ref.closed$.pipe(take(1), map((result) => result === true));
}

// AFTER
import { HlmAlertDialogService } from '@spartan-ng/helm/alert-dialog';
// ...
private readonly dialogService = inject(HlmAlertDialogService);
confirm(data: ConfirmDialogData): Observable<boolean> {
  const ref = this.dialogService.open(ConfirmDialogComponent, {
    context: data,
  });
  return ref.closed$.pipe(take(1), map((result) => result === true));
}
```

Note: Check that `HlmAlertDialogService` exists and has the same `.open()` API. If it doesn't exist as a service (Spartan alert-dialog may only support template-driven usage), keep the current `HlmDialogService` for opening and only change the component internals to use AlertDialog visuals. In that case, keep the DialogService as-is.

**Verify**: Delete confirmation dialog appears with proper styling. Clicking Cancel closes without action. Clicking Confirm executes the delete.

---

### 2.3 Rename SelectComponent to AsyncSelectComponent

**Current files**:
- `src/shared/components/select/select.component.ts`
- `src/shared/components/select/index.ts`

The custom select wraps Spartan select with `rxResource` for async option loading, spinner, and transloco. It needs a distinct name from raw Spartan select.

**Steps**:

1. Rename `src/shared/components/select/` directory to `src/shared/components/async-select/`
2. Rename `select.component.ts` to `async-select.component.ts`
3. Update class name: `SelectComponent` -> `AsyncSelectComponent`
4. Update selector: `app-select` -> `app-async-select`
5. Update `index.ts` to export from `./async-select.component`
6. Update all consumers (search for `app-select` in templates and `SelectComponent` / `from '../select'` / `from '../../select'` in TS):
   - `src/shared/components/entity-list-view/entity-list-view.component.ts` imports `SelectComponent` from `'../select'`
   - `src/shared/components/entity-list-view/entity-list-view.component.html` uses `<app-select ...>`
   - Any feature components that import and use it

Replace all `<app-select` with `<app-async-select` in templates. Replace all `SelectComponent` imports with `AsyncSelectComponent` and update the import path from `'../select'` to `'../async-select'`.

**Verify**: `ng build` passes. Filter dropdowns in list views still load options and respond to selection.

---

## Tier 3: Generate and Adopt New Helm Components

### 3.1 Generate new helms

Run from `apps/portals/landlord/web/landlord-portal/`:

```bash
npx ng g @spartan-ng/cli:ui table
npx ng g @spartan-ng/cli:ui card
npx ng g @spartan-ng/cli:ui checkbox
npx ng g @spartan-ng/cli:ui skeleton
npx ng g @spartan-ng/cli:ui avatar
npx ng g @spartan-ng/cli:ui switch
```

Each command generates a directory under `src/shared/components/ui/{name}/`. After all generate, add tsconfig path aliases in `tsconfig.json` for each:

```jsonc
"@spartan-ng/helm/table": ["./src/shared/components/ui/table/src/index.ts"],
"@spartan-ng/helm/card": ["./src/shared/components/ui/card/src/index.ts"],
"@spartan-ng/helm/checkbox": ["./src/shared/components/ui/checkbox/src/index.ts"],
"@spartan-ng/helm/skeleton": ["./src/shared/components/ui/skeleton/src/index.ts"],
"@spartan-ng/helm/avatar": ["./src/shared/components/ui/avatar/src/index.ts"],
"@spartan-ng/helm/switch": ["./src/shared/components/ui/switch/src/index.ts"]
```

If any require brain packages not already installed (checkbox, switch will need brain), install them:

```bash
npm install @spartan-ng/brain@alpha.637
```

(The brain package is a single package; individual sub-paths like `@spartan-ng/brain/checkbox` are entry points within it.)

**Verify**: `ng build` passes. New helm directories exist under `src/shared/components/ui/`.

---

### 3.2 Apply HlmTable to entity-list-view

**File**: `src/shared/components/entity-list-view/entity-list-view.component.html`

The desktop table (approximately L199-310) uses inline Tailwind classes on `<table>`, `<thead>`, `<tr>`, `<th>`, `<td>`. Replace with Spartan table directives.

```html
<!-- BEFORE (~L199) -->
<div class="overflow-hidden rounded-lg border bg-card">
  <table class="w-full">
    <thead>
      @for (headerGroup of table.getHeaderGroups(); track headerGroup.id) {
        <tr class="border-b bg-muted/50">
          @for (header of headerGroup.headers; track header.id) {
            <th class="px-4 py-3 text-left">
              ...
            </th>
          }
          ...
        </tr>
      }
    </thead>
    <tbody>
      @for (row of table.getRowModel().rows; track getEntityId(row.original)) {
        <tr class="border-b transition-colors hover:bg-muted/50 cursor-pointer" ...>
          @for (cell of row.getVisibleCells(); track cell.id) {
            <td class="px-4 py-3">
              ...
            </td>
          }
          ...
        </tr>
      }
    </tbody>
  </table>
</div>

<!-- AFTER -->
<div class="overflow-hidden rounded-lg border bg-card">
  <table hlmTable>
    <thead>
      @for (headerGroup of table.getHeaderGroups(); track headerGroup.id) {
        <tr hlmTrow class="bg-muted/50">
          @for (header of headerGroup.headers; track header.id) {
            <th hlmTh>
              ...
            </th>
          }
          ...
        </tr>
      }
    </thead>
    <tbody>
      @for (row of table.getRowModel().rows; track getEntityId(row.original)) {
        <tr hlmTrow class="cursor-pointer" ...>
          @for (cell of row.getVisibleCells(); track cell.id) {
            <td hlmTd>
              ...
            </td>
          }
          ...
        </tr>
      }
    </tbody>
  </table>
</div>
```

Strip inline classes that HlmTable/HlmTrow/HlmTh/HlmTd already provide (`border-b`, `px-4 py-3`, `text-left`, `transition-colors`, `hover:bg-muted/50`). Keep classes NOT provided by the helms (like `cursor-pointer`, `bg-muted/50` on header rows if needed).

Check the generated `HlmTable`, `HlmTrow`, `HlmTh`, `HlmTd` CVA definitions in `src/shared/components/ui/table/src/lib/` to see which classes they already apply. Only add overrides for classes they don't cover.

**TS**: Add `HlmTableImports` from `@spartan-ng/helm/table` to the imports array.

**Verify**: Table renders with proper styling, borders, hover states. No visual regression.

---

### 3.3 Apply HlmCard across the codebase

Replace inline `rounded-lg border bg-card shadow-sm` patterns with Spartan card directives.

**Target files and patterns**:

1. **Mobile cards in entity-list-view** (approximately L315-378):

   ```html
   <!-- BEFORE -->
   <div class="rounded-lg border bg-card p-4 transition-shadow hover:shadow-md cursor-pointer" ...>

   <!-- AFTER -->
   <div hlmCard class="cursor-pointer transition-shadow hover:shadow-md" ...>
     <div hlmCardContent class="p-4">
   ```

2. **Detail page info cards** (all 4 detail component HTML files):

   Wherever `class="rounded-lg border bg-card ..."` appears wrapping detail sections, replace with `hlmCard`, `hlmCardHeader`, `hlmCardContent`. Search each detail template for this pattern.

3. **Accordion card** in `property-details.component.html` (approximately L127):

   ```html
   <!-- BEFORE -->
   <div hlmAccordion class="rounded-lg border bg-card shadow-sm">

   <!-- AFTER -->
   <div hlmAccordion hlmCard>
   ```

Import `HlmCardImports` from `@spartan-ng/helm/card` in each updated component's TS file.

**Verify**: Cards have consistent styling across mobile and desktop views. No visual regression.

---

### 3.4 Apply HlmCheckbox to column visibility

**File**: `src/shared/components/entity-list-view/entity-list-view.component.html`

The column drawer (approximately L503-510) has a raw `<input type="checkbox">`.

```html
<!-- BEFORE -->
<label class="flex cursor-pointer items-center justify-between p-2 rounded-md hover:bg-accent">
  <span class="text-sm">
    {{ column.columnDef.header }}
  </span>
  <input
    type="checkbox"
    [checked]="column.getIsVisible()"
    (change)="column.toggleVisibility()"
    class="h-4 w-4 rounded border-gray-300 text-primary focus:ring-2 focus:ring-ring focus:ring-offset-2 cursor-pointer" />
</label>

<!-- AFTER -->
<label
  class="flex cursor-pointer items-center justify-between p-2 rounded-md hover:bg-accent"
  (click)="column.toggleVisibility(); $event.preventDefault()">
  <span class="text-sm">
    {{ column.columnDef.header }}
  </span>
  <hlm-checkbox [checked]="column.getIsVisible()" />
</label>
```

Check the generated `HlmCheckbox` API (`checked` input, `changed` output). If the API differs, adjust accordingly. The checkbox doesn't need two-way binding here since `column.toggleVisibility()` handles the state.

Import `HlmCheckboxImports` from `@spartan-ng/helm/checkbox` (or equivalent — check the generated index.ts).

**Verify**: Column visibility checkboxes toggle correctly. Checkbox renders with Spartan styling.

---

### 3.5 Apply HlmSkeleton to loading states

Replace spinner-based loading with skeleton placeholders for initial data loads. Keep `HlmSpinner` for in-progress actions (form submit, refresh).

**File**: `src/shared/components/entity-list-view/entity-list-view.component.html`

Replace the loading overlay (approximately L189-194) with skeleton rows when `state.loading && !state.hasData` (initial load):

```html
<!-- Replace loading overlay with skeleton for initial load -->
@if (state.loading && !state.hasData) {
  <div class="overflow-hidden rounded-lg border bg-card">
    <table hlmTable>
      <thead>
        <tr hlmTrow class="bg-muted/50">
          @for (_ of [1,2,3,4]; track $index) {
            <th hlmTh><hlm-skeleton class="h-4 w-24" /></th>
          }
        </tr>
      </thead>
      <tbody>
        @for (_ of [1,2,3,4,5]; track $index) {
          <tr hlmTrow>
            @for (_ of [1,2,3,4]; track $index) {
              <td hlmTd><hlm-skeleton class="h-4 w-full" /></td>
            }
          </tr>
        }
      </tbody>
    </table>
  </div>
}
```

Keep the existing spinner overlay for subsequent loads (data already showing, e.g., page change, filter apply):

```html
@if (state.loading && state.hasData) {
  <div class="absolute inset-0 z-10 flex items-center justify-center rounded-lg bg-background/80 backdrop-blur-sm">
    <hlm-spinner size="lg" />
  </div>
}
```

Import `HlmSkeleton` (or equivalent) from `@spartan-ng/helm/skeleton`.

The `HlmSkeleton` is a styling-only component (no brain needed). Check the generated component's selector (likely `hlm-skeleton` or `[hlmSkeleton]`).

**Verify**: Initial list load shows skeleton rows. Subsequent page changes show spinner overlay on top of existing data.

---

### 3.6 Apply HlmAvatar to header user button

**File**: `src/app/layout/header/header.component.ts`

The user button (approximately L55-60 in the inline template) currently uses an icon in a circular button.

```html
<!-- BEFORE -->
<button hlmBtn variant="ghost" size="icon"
  [hlmDropdownMenuTrigger]="userMenu"
  class="rounded-full bg-muted hover:bg-accent">
  <app-icon name="account_circle" [size]="20" />
</button>

<!-- AFTER -->
<button
  [hlmDropdownMenuTrigger]="userMenu"
  class="cursor-pointer">
  <hlm-avatar>
    <span hlmAvatarFallback class="bg-muted text-sm font-medium">
      {{ initials() }}
    </span>
  </hlm-avatar>
</button>
```

Add a computed signal for user initials:

```typescript
protected readonly initials = computed(() => {
  const name = this.sessionService.userName();
  if (!name) return '?';
  const parts = name.split(' ');
  return parts.length > 1
    ? (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
    : parts[0][0].toUpperCase();
});
```

Import `HlmAvatarImports` from `@spartan-ng/helm/avatar` (check generated exports). Add `computed` to the Angular core import if not already there.

**Verify**: Header shows user initials (e.g., "JD" for "John Doe") in a circular avatar. Dropdown menu still opens.

---

### 3.7 Apply HlmSwitch to theme toggle

**File**: `src/app/core/components/theme-toggle/theme-toggle.component.ts`

Replace emoji-based button with a switch:

```typescript
// BEFORE
imports: [HlmButton],
template: `
  <button hlmBtn variant="ghost" size="icon"
    (click)="toggleTheme()"
    [title]="isDarkMode() ? 'Switch to light mode' : 'Switch to dark mode'">
    {{ isDarkMode() ? '☀️' : '🌙' }}
  </button>
`

// AFTER
imports: [HlmSwitchImports, IconComponent],
template: `
  <label class="flex items-center gap-2 cursor-pointer">
    <app-icon [name]="isDarkMode() ? 'dark_mode' : 'light_mode'" [size]="16" />
    <brn-switch [checked]="isDarkMode()" (changed)="toggleTheme()">
      <hlm-switch-thumb />
    </brn-switch>
  </label>
`
```

Check the generated switch helm API. If `HlmSwitchImports` doesn't exist, import individual components. The brain switch package is `@spartan-ng/brain/switch`.

Import `HlmSwitchImports` from `@spartan-ng/helm/switch` and `IconComponent`.

Also update the header's inline theme toggle (approximately L94-103 in `header.component.ts` template). The header uses a dropdown menu item for theme toggle. Keep it as a clickable menu item but replace the emoji with proper icon rendering (the icons `light_mode` and `dark_mode` are already in ICON_MAP).

**Verify**: Theme toggle renders as a switch on landing page. Header menu item toggles correctly. Dark/light mode applies.

---

### 3.8 Apply hlmBtn to unstyled buttons

**File**: `src/shared/components/entity-list-view/entity-list-view.component.html`

Several buttons lack `hlmBtn`:

1. **Sort header buttons** (approximately L210): Add `hlmBtn variant="ghost" size="sm"`
   ```html
   <!-- BEFORE -->
   <button class="flex items-center gap-1 font-semibold hover:text-primary" ...>

   <!-- AFTER -->
   <button hlmBtn variant="ghost" size="sm" class="flex items-center gap-1 font-semibold" ...>
   ```

2. **Search clear buttons** (approximately L87, L170): Add `hlmBtn variant="ghost" size="icon"`
   ```html
   <!-- BEFORE -->
   <button type="button" (click)="clearTableSearch()"
     class="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground">

   <!-- AFTER -->
   <button hlmBtn variant="ghost" size="icon" type="button" (click)="clearTableSearch()"
     class="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground">
   ```

**Verify**: Sort buttons and clear buttons have consistent hover/focus states.

---

### 3.9 Apply hlmInput to search and filter inputs

**File**: `src/shared/components/entity-list-view/entity-list-view.component.html`

1. **Desktop search input** (approximately L77-85):
   ```html
   <!-- BEFORE -->
   <input
     type="text"
     ...
     class="h-10 w-full rounded-md border border-input bg-background pl-10 pr-10 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2" />

   <!-- AFTER -->
   <input
     hlmInput
     type="text"
     ...
     class="w-full pl-10 pr-10" />
   ```

2. **Mobile search input** (approximately L163): Same change.

3. **Filter text input** (approximately L436):
   ```html
   <!-- BEFORE -->
   <input ... class="h-10 w-full rounded-md border border-input bg-background px-3 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2" />

   <!-- AFTER -->
   <input hlmInput ... class="w-full" />
   ```

4. **Filter number input** (approximately L455): Same change.

Import `HlmInput` from `@spartan-ng/helm/input` in the TS file.

**Verify**: Inputs render with Spartan styling. Focus rings display correctly.

---

## Tier 4: Migrate Drawers to Sheet

### 4.1 Migrate create-drawer components to HlmSheet

All 3 create-drawer components follow the same pattern. Apply to each:
- `src/app/features/properties/create-drawer/create-property-drawer.component.ts` + `.html`
- `src/app/features/buildings/create-drawer/create-building-drawer.component.ts` + `.html`
- `src/app/features/companies/create-drawer/create-company-drawer.component.ts` + `.html`

Each parent component has a signal (e.g., `createDrawerOpen`) and passes it as `[open]` input. The drawer emits `(closed)` when dismissed.

**Approach**: The Spartan Sheet is overlay-based. Convert each create-drawer to use `HlmSheet` by wrapping the trigger + content. There are two viable patterns:

**Pattern A - Programmatic** (if `BrnSheetService` exists):

```typescript
private sheetService = inject(BrnSheetService);

openCreateDrawer(): void {
  this.sheetService.open(CreatePropertyDrawerComponent, {
    side: 'right',
    // additional config
  });
}
```

**Pattern B - Template-driven** (preferred for Spartan, matches shadcn pattern):

The parent component wraps the trigger and content:

```html
<hlm-sheet side="right">
  <button hlmSheetTrigger hlmBtn>
    <app-icon name="add" [size]="20" />
    <span>{{ 'properties.newProperty' | transloco }}</span>
  </button>

  <hlm-sheet-content *hlmSheetPortal class="sm:max-w-md w-full">
    <hlm-sheet-header>
      <h2 hlmSheetTitle>{{ 'properties.newProperty' | transloco }}</h2>
    </hlm-sheet-header>

    <!-- Form content (the bulk of the current drawer template) -->
    <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-1 flex-col">
      <div class="flex-1 space-y-6 overflow-y-auto px-6 py-6">
        ...form fields...
      </div>
      <hlm-sheet-footer>
        <button hlmBtn variant="outline" type="button" hlmSheetClose>
          {{ 'common.cancel' | transloco }}
        </button>
        <button hlmBtn type="submit" [disabled]="form.invalid || saving()">
          @if (saving()) { <hlm-spinner size="sm" /> }
          {{ 'common.create' | transloco }}
        </button>
      </hlm-sheet-footer>
    </form>
  </hlm-sheet-content>
</hlm-sheet>
```

For this approach, the create-drawer's form logic can be inlined into the parent component or kept as a child component rendered inside `hlm-sheet-content`.

**Decision**: Check whether `BrnSheetService` exists in `@spartan-ng/brain/sheet`. If it provides a programmatic open like `HlmDialogService`, use Pattern A to minimize refactoring (keep separate component files). If only template-driven, use Pattern B and restructure the create-drawer content to be rendered inside the Sheet in the parent.

Regardless of pattern:
- Remove the hand-rolled backdrop (`fixed inset-0 z-50 bg-background/80`)
- Remove the hand-rolled slide panel (`fixed inset-y-0 right-0 z-50`)
- Remove `DrawerFooterDirective` import from each component
- Remove manual open/close animation classes (`translate-x-full`, `translate-x-0`)

**TS changes** for each create-drawer component:

```typescript
// REMOVE
import { DrawerFooterDirective } from '../../../../shared/components/drawer-footer';
// ADD
import { HlmSheetImports } from '@spartan-ng/helm/sheet';
```

Update `imports` array accordingly.

**Verify**: Each create drawer opens from the right side with smooth animation and backdrop. Close works via cancel button, close icon, and backdrop click. Form submission works.

---

### 4.2 Migrate entity-list-view filter and column drawers

**File**: `src/shared/components/entity-list-view/entity-list-view.component.html`

The filter drawer (approximately L392-477) and column drawer (approximately L480-528) use the same hand-rolled pattern.

For each, replace with template-driven Sheet. Since these are inline in the template (not separate components), use the template-driven pattern:

**Filter drawer**:

```html
<!-- Wrap filter button as trigger -->
<hlm-sheet side="right">
  <button hlmSheetTrigger hlmBtn variant="outline" size="icon"
    [attr.aria-label]="'common.filters' | transloco">
    <app-icon name="filter_list" [size]="20" />
  </button>

  <hlm-sheet-content *hlmSheetPortal class="w-80">
    <hlm-sheet-header>
      <h2 hlmSheetTitle>{{ 'common.filters' | transloco }}</h2>
    </hlm-sheet-header>

    <div class="flex-1 overflow-y-auto p-4">
      <!-- filter fields (same as current content) -->
    </div>

    <hlm-sheet-footer>
      <button hlmBtn variant="outline" class="flex-1" (click)="clearFilters()">
        {{ 'common.clearFilters' | transloco }}
      </button>
      <button hlmBtn class="flex-1" hlmSheetClose (click)="applyFilters()">
        {{ 'common.applyFilters' | transloco }}
      </button>
    </hlm-sheet-footer>
  </hlm-sheet-content>
</hlm-sheet>
```

Apply same pattern for the column drawer.

This changes the drawer open/close mechanism from manual signals (`filterDrawerOpen`, `columnDrawerOpen`, `toggleFilterDrawer()`, `toggleColumnDrawer()`) to Spartan Sheet's built-in state management. Remove the unused signals and toggle methods from the TS file.

Import `HlmSheetImports` from `@spartan-ng/helm/sheet` in the TS file.

**Verify**: Filter and column drawers open/close with smooth animations. Filter application works. Column visibility toggles persist.

---

### 4.3 Migrate mobile navigation drawer

**File**: `src/app/layout/layout.component.ts`

The mobile sidebar (approximately L60-72 in inline template) uses a hand-rolled drawer.

```html
<!-- BEFORE -->
@if (responsive.isMobile() && mobileDrawerOpen()) {
  <div class="fixed inset-0 z-50 bg-background/80 backdrop-blur-sm"
    (click)="closeMobileDrawer()">
    <div class="fixed inset-y-0 left-0 w-64 bg-background shadow-lg"
      (click)="$event.stopPropagation()">
      <app-navigation [menuItems]="menuItems" [collapsed]="false" [showLogo]="true" />
    </div>
  </div>
}

<!-- AFTER -->
<hlm-sheet side="left">
  <!-- Move the burger button to be the sheet trigger -->
  <!-- The burger button is in header.component, so we need a different approach -->
</hlm-sheet>
```

The burger menu button is in `header.component.ts` and emits `menuToggle`. The layout component currently uses `mobileDrawerOpen` signal to show/hide. For Sheet integration, there are two approaches:

**Approach A**: Keep the signal-based open/close but use `BrnSheetTrigger` programmatically. If `BrnSheetService` exists, inject it and call `open()` from the `toggleMobileDrawer()` method.

**Approach B**: Move the Sheet wrapper into the layout template and use a `ViewChild` reference to programmatically open it when `menuToggle` fires.

Choose the approach that works with the Spartan Sheet API (check `@spartan-ng/brain/sheet` exports). The mobile drawer opens from the left (`side="left"`) and has width `w-64`.

Import `HlmSheetImports` in the layout component.

Remove `mobileDrawerOpen` signal and `toggleMobileDrawer()`/`closeMobileDrawer()` methods.

**Verify**: Mobile burger menu opens the navigation drawer from the left with backdrop and animation. Clicking outside closes it.

---

### 4.4 Delete drawer components

After all drawer migrations are complete:

1. Delete `src/shared/components/drawer/` (contains only `README.md`)
2. Delete `src/shared/components/drawer-footer/` (contains `drawer-footer.directive.ts` + `index.ts`)

Verify no imports reference either directory.

**Verify**: `ng build` passes.

---

## Tier 5: Form Field Consolidation

### 5.1 Replace `class="input"` with `hlmInput` in detail pages

**Target**: 7 `<input>` elements using `class="input w-full"` across 3 detail components + header.

**Files and changes**:

1. `src/app/features/companies/details/company-details.component.html`:
   - L47: `class="input w-full font-mono uppercase"` -> `hlmInput class="w-full font-mono uppercase"`
   - L84: `class="input w-full"` -> `hlmInput class="w-full"`

2. `src/app/features/buildings/details/building-details.component.html`:
   - L48: `class="input w-full"` -> `hlmInput class="w-full"`
   - L72: `class="input w-full"` -> `hlmInput class="w-full"`

3. `src/app/features/properties/details/property-details.component.html`:
   - L57: `class="input w-full"` -> `hlmInput class="w-full"`
   - L82: `class="input w-full"` -> `hlmInput class="w-full"`
   - L107 (textarea): `class="input w-full"` -> `hlmTextarea class="w-full"`

4. `src/app/layout/header/header.component.ts` (inline template):
   - L46: `class="input w-full"` -> `hlmInput class="w-full"`

Import `HlmInput` from `@spartan-ng/helm/input` (and `HlmTextarea` from `@spartan-ng/helm/textarea` where used) in each component's TS file.

**Verify**: Inputs render with consistent Spartan styling. Focus rings display correctly.

---

### 5.2 Replace appTextInput + app-validation-error in create drawers

Each of the 3 create-drawer components uses `appTextInput` on inputs and `<app-validation-error>` for validation messages.

**For each create-drawer** (`create-property-drawer`, `create-building-drawer`, `create-company-drawer`):

1. Replace `appTextInput` with `hlmInput`:
   ```html
   <!-- BEFORE -->
   <input id="code" formControlName="code" appTextInput class="w-full" ... />

   <!-- AFTER -->
   <input id="code" formControlName="code" hlmInput class="w-full" ... />
   ```

2. Wrap each form field group in `<hlm-form-field>` with a label:
   ```html
   <!-- BEFORE -->
   <div class="space-y-2">
     <label for="code" class="text-sm font-medium">
       {{ 'field.code' | transloco }}
       <span class="text-destructive">*</span>
     </label>
     <input id="code" formControlName="code" appTextInput class="w-full" />
     @if (form.controls.code.invalid && form.controls.code.touched) {
       <app-validation-error [message]="'validation.required' | transloco" />
     }
   </div>

   <!-- AFTER -->
   <hlm-form-field>
     <label hlmLabel for="code">
       {{ 'field.code' | transloco }}
       <span class="text-destructive">*</span>
     </label>
     <input id="code" formControlName="code" hlmInput class="w-full" />
     @if (form.controls.code.invalid && form.controls.code.touched) {
       <hlm-error>{{ 'validation.required' | transloco }}</hlm-error>
     }
   </hlm-form-field>
   ```

3. **Special case: create-company-drawer** has `variant="info"` and `variant="success"` validation states for async code/name checking. `HlmError` only handles error state. For info/success messages, use plain `<p>` elements:
   ```html
   @if (form.controls.code.pending) {
     <p class="mt-1 flex items-center gap-1 text-sm text-muted-foreground">
       <app-icon name="hourglass_empty" [size]="14" />
       {{ 'companies.checkingCode' | transloco }}
     </p>
   }
   @if (form.controls.code.valid && form.controls.code.dirty) {
     <p class="mt-1 flex items-center gap-1 text-sm text-green-600">
       <app-icon name="check_circle" [size]="14" />
       {{ 'companies.codeAvailable' | transloco }}
     </p>
   }
   ```

**TS changes** for each create-drawer:

```typescript
// REMOVE
import { TextInputDirective } from '../../../../shared/components/form-field';
import { ValidationErrorComponent } from '../../../../shared/components/form-field';
// (or combined import)
import { DrawerFooterDirective } from '../../../../shared/components/drawer-footer';

// ADD
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmFormFieldImports } from '@spartan-ng/helm/form-field';
import { HlmLabel } from '@spartan-ng/helm/label';
```

Update `imports` array to swap old for new.

**Verify**: Form fields display labels, inputs, and validation errors correctly. Async validators in create-company-drawer show checking/available/taken states. Form submission works.

---

### 5.3 Delete custom form-field components

After steps 5.1 and 5.2, the custom form-field components have zero usages.

Delete the entire directory:
- `src/shared/components/form-field/text-input.directive.ts`
- `src/shared/components/form-field/validation-error.component.ts`
- `src/shared/components/form-field/index.ts`

Verify no imports reference `shared/components/form-field`.

**Verify**: `ng build` passes.

---

## Tier 6: Dead Code Cleanup

### 6.1 Clean styles.css

**File**: `src/styles.css`

Remove the entire `@layer components` block (L70-155) and the Material compat `:root` block (L65-68). After this, the file should contain only:

```css
@import "tailwindcss";
@import "@spartan-ng/brain/hlm-tailwind-preset.css";

@theme {
  /* color variables (keep as-is) */
}

@layer base {
  * {
    @apply border-border outline-ring/50;
  }
  body {
    @apply bg-background text-foreground;
  }

  .dark {
    /* dark mode overrides (keep as-is) */
  }
}
```

The removed classes and their replacements:

| Dead Class | Replaced By |
|---|---|
| `.btn`, `.btn-*` | `hlmBtn` directive (zero usages of CSS class) |
| `.input` | `hlmInput` directive (migrated in Tier 5) |
| `.card`, `.card-*` | `hlmCard` directives (migrated in Tier 3) |
| `.listbox`, `.listbox-option*` | `HlmSelect` / `HlmAutocomplete` |
| `.badge-*` | `StatusBadgeDirective` (uses own classes) |
| `.toast-panel` | ngx-sonner |
| `.dialog-panel` | `HlmDialog` / `HlmAlertDialog` |
| `.mat-mdc-dialog-surface` | Material removed |
| `--mat-sys-*` / `--mat-mdc-*` | Material removed |

**Verify**: `ng build` passes. No visual regressions (all replaced in prior steps).

---

### 6.2 Remove dead .tab-trigger CSS from detail components

Delete ALL content from these CSS files (they contain only dead `.tab-trigger` rules from pre-Spartan Angular ARIA tabs):

1. `src/app/features/properties/details/property-details.component.css` (20 lines)
2. `src/app/features/buildings/details/building-details.component.css` (20 lines)
3. `src/app/features/companies/details/company-details.component.css` (24 lines)
4. `src/app/features/organizations/details/organization-details.component.css` (57 lines, also has dead `.input` class)

After emptying each file, remove the `styleUrl` (or `styleUrls`) property from each component's `@Component` decorator in the corresponding TS file. Then delete the empty CSS file.

**Verify**: `ng build` passes. Tab styling is unchanged (driven by `hlmTabsTrigger`/`hlmTabsList`).

---

### 6.3 Fix organization tier badge

**File**: `src/app/features/organizations/details/organization-details.component.html`

The tier badge (approximately L59) uses inline Tailwind classes (`bg-blue-100 text-blue-800 ...`) instead of the `StatusBadgeDirective` used by other detail views.

Replace with `StatusBadgeDirective` or `hlmBadge variant="secondary"` for consistency with other detail pages. Check how other detail views render their badges to match the pattern.

**Verify**: Tier badge renders consistently with badges in other detail views.

---

### 6.4 Verify no remaining CDK overlay/menu usage

Run:

```bash
grep -r "OverlayModule\|cdkMenu\|cdkMenuItem\|cdkMenuTrigger\|cdkToolbar" src/ --include="*.ts" --include="*.html"
```

Expected: zero results. If any remain, migrate them following the patterns in Tier 2.

Note: `@angular/cdk` itself must stay installed. Spartan brain depends on CDK packages (`@angular/cdk/a11y`, `@angular/cdk/bidi`, `@angular/cdk/coercion`, etc.). DO NOT uninstall CDK.

---

## Verification Checklist

Run after all tiers are complete:

- [ ] `ng build` succeeds with zero errors
- [ ] `grep -r "OverlayModule\|cdkMenu\|cdkToolbar" src/` returns zero results
- [ ] `grep -r "class=\"input " src/ --include="*.html"` returns zero results
- [ ] `grep -r "appTextInput\|appButton\|appDrawerFooter" src/` returns zero results
- [ ] `grep -r "class=\"btn " src/ --include="*.html"` returns zero results
- [ ] No `@angular/material` or `@angular/aria` in `package.json`
- [ ] New helm directories exist: `table`, `card`, `checkbox`, `skeleton`, `avatar`, `switch`
- [ ] New tsconfig path aliases exist for all 6 new helms
- [ ] Navigation sidebar: all 5 nav icons render (dashboard, companies, properties, buildings, organization)
- [ ] List views: sort icons render (up/down/unsorted), row action icons render (edit, delete), empty state icon renders
- [ ] List views: create button uses SPA navigation (no full page reload)
- [ ] List views: table has Spartan table styling, mobile cards use `hlmCard`
- [ ] List views: column drawer uses HlmCheckbox, filter/column drawers use HlmSheet
- [ ] List views: initial load shows skeleton, subsequent loads show spinner overlay
- [ ] Detail views: overflow menu uses HlmDropdownMenu (not CDK menu)
- [ ] Detail views: inputs use `hlmInput`, cards use `hlmCard`
- [ ] Detail views: no dead CSS files remain
- [ ] Property details: accordion opens without runtime error, "New Building" button styled
- [ ] Create drawers: open as HlmSheet from right, form fields use HlmFormField + hlmInput
- [ ] Create company: async code check shows hourglass/checkmark icons
- [ ] Confirm dialog: renders as AlertDialog with proper action/cancel semantics
- [ ] Autocomplete: shows translated placeholder text
- [ ] Header: user avatar shows initials, dropdown works
- [ ] Theme: switch toggle works on landing page, menu toggle works in header
- [ ] Organization audit log: "Linked" event shows translated text
- [ ] Dark mode: toggle and verify all Spartan components respect dark theme
- [ ] Mobile: burger menu opens navigation Sheet from left, responsive layout works
- [ ] No dead components remain: `drawer-footer/`, `drawer/`, `form-field/` deleted
- [ ] `styles.css` has no `@layer components` block
