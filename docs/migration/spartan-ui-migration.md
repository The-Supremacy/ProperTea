# Migration: Spartan UI Adoption

This document provides a phased migration checklist for replacing the current Angular Aria + Material + CVA stack with Spartan UI, per [ADR 0016](../decisions/0016-spartan-ui-adoption.md).

Read this document fully before beginning migration. Each phase is designed to leave the application in a buildable, functional state.

## Pre-Migration State

### Current dependencies to remove
| Package | Used For |
|---|---|
| `@angular/material` | Dialog, Snackbar, Icon |
| `@angular/aria` | Tabs, Accordion, Menu, Toolbar, Combobox, Listbox |

### Current dependencies to retain
| Package | Reason |
|---|---|
| `@angular/cdk` | Spartan depends on it; also used for Overlay, Layout/BreakpointObserver |
| `@tanstack/angular-table` | Spartan Data Table integrates with it |
| `class-variance-authority` | Used by Spartan Helm |
| `clsx` + `tailwind-merge` | Used by Spartan's `hlm()` utility |
| `embla-carousel-angular` | Spartan Carousel uses it |
| `lucide-angular` | Icons (evaluate switching to `ng-icons` during Phase 3) |

### Files that import `@angular/material` (4 files)
| File | Imports |
|---|---|
| `src/shared/components/icon/icon.component.ts` | `MatIconModule` |
| `src/shared/components/confirm-dialog/confirm-dialog.component.ts` | `MAT_DIALOG_DATA`, `MatDialogRef`, `MatDialogModule` |
| `src/app/core/services/dialog.service.ts` | `MatDialog` |
| `src/app/core/services/toast.service.ts` | `MatSnackBar`, `MatSnackBarConfig` |

### Files that import `@angular/aria` (7 files, 12 import statements)
| File | Aria Modules |
|---|---|
| `src/app/features/properties/details/property-details.component.ts` | `tabs`, `accordion` |
| `src/app/features/buildings/details/building-details.component.ts` | `tabs` |
| `src/app/features/companies/details/company-details.component.ts` | `tabs` |
| `src/app/features/organizations/details/organization-details.component.ts` | `tabs` |
| `src/app/layout/header/header.component.ts` | `menu` |
| `src/shared/components/entity-list-view/entity-list-view.component.ts` | `menu`, `toolbar` |
| `src/shared/components/select/select.component.ts` | `combobox`, `listbox` |
| `src/shared/components/autocomplete/autocomplete.component.ts` | `combobox`, `listbox` |

### Shared components that will change
| Component | Current Implementation | Spartan Replacement |
|---|---|---|
| `ButtonDirective` (`[appBtn]`) | CVA directive, `button/button.directive.ts` | Spartan Button |
| `BadgeDirective` (`[appBadge]`) | CVA directive, `badge/badge.directive.ts` | Spartan Badge |
| `SelectComponent` (`app-select`) | Aria Combobox + Listbox + CDK Overlay, 130 lines | Spartan Select |
| `AutocompleteComponent` (`app-autocomplete`) | Aria Combobox + Listbox + CDK Overlay, 125 lines | Spartan Combobox/Autocomplete |
| `ConfirmDialogComponent` | Material Dialog | Spartan Alert Dialog |
| `IconComponent` (`app-icon`) | Material Icon | Spartan Icon or keep Lucide directly |
| `SpinnerComponent` (`app-spinner`) | Custom SVG animation | Spartan Spinner |
| `StatusBadgeComponent` | CVA component | Keep as-is (business component with domain variants) |
| `TextInputDirective` (`[appTextInput]`) | CVA directive | Spartan Input |
| `ValidationErrorComponent` | CVA component | Spartan Form Field / keep custom |

### Services that will change
| Service | Current | Spartan Replacement |
|---|---|---|
| `DialogService` | Material `MatDialog` | Spartan Dialog (brain) |
| `ToastService` | Material `MatSnackBar` | Spartan Sonner (`ngx-sonner`) |

### Styles that will change
| File | Changes |
|---|---|
| `src/styles.css` | Remove `@angular/cdk/overlay-prebuilt.css` import (included in Spartan preset), remove Material bridge variables, remove Material override classes, add Spartan preset import |

---

## Phase 0: Install Spartan and Generate Helm Components

This phase adds Spartan to the project without modifying any existing code. Both old and new can coexist temporarily.

### 0.1 Install Spartan CLI

```bash
cd apps/portals/landlord/web/landlord-portal
npm install -D @spartan-ng/cli
```

### 0.2 Initialize Spartan

```bash
npx ng g @spartan-ng/cli:init
```

This will:
- Install `@spartan-ng/brain` as a dependency
- Create a `components.json` configuration file
- Set up a `hlm()` utility (if it differs from existing `cn()`, consolidate to one)

### 0.3 Generate Theme

```bash
npx ng g @spartan-ng/cli:ui-theme
```

Compare the generated CSS variables against the existing `@theme` block in `src/styles.css`. The current theme uses HSL values (`hsl(222.2 84% 4.9%)`); Spartan defaults to OKLCH. Choose one format and apply it consistently. The existing color palette should be preserved - only the format and variable naming convention should change.

### 0.4 Generate Helm Components

Generate all components needed for current and near-future usage:

```bash
npx ng g @spartan-ng/cli:ui
```

Select these components when prompted:
- **Accordion** (replaces Aria Accordion)
- **Alert Dialog** (replaces Material Dialog for confirms)
- **Autocomplete** (replaces custom Aria Combobox/Listbox autocomplete)
- **Badge** (replaces CVA BadgeDirective)
- **Breadcrumb** (replaces custom breadcrumb)
- **Button** (replaces CVA ButtonDirective)
- **Card** (replaces CSS `.card` classes)
- **Combobox** (for future use)
- **Data Table** (replaces custom TanStack integration)
- **Dialog** (replaces Material Dialog)
- **Dropdown Menu** (replaces Aria Menu)
- **Form Field** (replaces custom form field)
- **Icon** (evaluate; may keep Lucide instead)
- **Input** (replaces CVA TextInputDirective and CSS `.input` class)
- **Label** (for form labels)
- **Pagination** (replaces custom pagination)
- **Select** (replaces custom Aria Combobox/Listbox select)
- **Separator** (replaces manual `<div role="separator">` elements)
- **Sheet** (replaces drawer placeholder / CDK Overlay side panels)
- **Skeleton** (for loading states)
- **Sonner** (replaces Material Snackbar / ToastService)
- **Spinner** (replaces custom SpinnerComponent)
- **Table** (base table styling)
- **Tabs** (replaces Aria Tabs)
- **Tooltip** (for future use)

Helm components will be generated into `src/shared/components/ui/` (or the path configured in `components.json`).

### 0.5 Verify Build

```bash
npx ng build
```

The build should pass. No existing code has been modified. Spartan components exist alongside the old ones.

### 0.6 Review Generated Code

Inspect the generated Helm components in `src/shared/components/ui/`. They should use `cva()` and a `hlm()` utility similar to the existing `cn()` utility. If both exist, decide whether to:
- Keep `cn()` and alias `hlm = cn` in the Spartan components
- Replace `cn()` with `hlm()` throughout
- Keep both (they do the same thing: `clsx` + `tailwind-merge`)

Consolidating to one utility is preferred. Update the `tailwindCSS.classFunctions` VS Code setting to include whichever you keep.

---

## Phase 1: Migrate Shared Primitives

Migrate the lowest-level shared components first. These are used across all features.

### 1.1 Button

**Current**: `src/shared/components/button/button.directive.ts` - CVA directive with `[appBtn]` selector, variants: `default`, `destructive`, `outline`, `secondary`, `ghost`, `link`. Sizes: `default`, `sm`, `lg`, `icon`.

**Target**: Replace with Spartan Button Helm directive. The Spartan Button is also a directive-based approach (`hlmBtn`), so the migration is largely a selector rename.

**Steps**:
1. Compare existing `buttonVariants` CVA definition with Spartan's generated Button Helm. Merge any custom variants (the existing ones likely match Shadcn defaults).
2. Update the `[appBtn]` selector in the generated Spartan Button to `[appBtn]` OR rename all usages to `[hlmBtn]`. Choose one approach and apply consistently. Using `[hlmBtn]` is recommended for consistency with Spartan docs and examples.
3. Delete `src/shared/components/button/button.directive.ts` and re-export Spartan's Button from `src/shared/components/button/index.ts`.
4. Update all import paths (if keeping the same barrel export path, only the internal implementation changes).

**Files importing ButtonDirective** (search for `from '../button'`, `from '../../button'`, etc.):
- `src/shared/components/confirm-dialog/confirm-dialog.component.ts`
- `src/shared/components/entity-details-view/entity-details-view.component.ts`
- `src/shared/components/entity-list-view/entity-list-view.component.ts`
- `src/app/layout/header/header.component.ts`
- `src/app/features/properties/details/property-details.component.ts`
- `src/app/features/buildings/details/building-details.component.ts` (likely)
- `src/app/features/companies/details/company-details.component.ts` (likely)
- `src/app/features/buildings/create-drawer/create-building-drawer.component.ts` (likely)
- `src/app/features/properties/create-drawer/create-property-drawer.component.ts` (likely)
- `src/app/features/companies/create-drawer/create-company-drawer.component.ts` (likely)

If keeping the same barrel export (`shared/components/button`), re-exporting the Spartan directive under the original name minimizes churn.

**Verify**: `ng build` passes. All buttons render with correct variants.

### 1.2 Badge

**Current**: `src/shared/components/badge/badge.directive.ts` - CVA directive with `[appBadge]` selector. Variants: `default`, `secondary`, `destructive`, `outline`.

**Target**: Spartan Badge Helm directive.

**Steps**: Same approach as Button. Merge variants, replace implementation, re-export.

**Note**: `StatusBadgeComponent` (`src/shared/components/status-badge/status-badge.component.ts`) is a *business component* with domain-specific variants (`success`, `warning`, `error`, `info`, `default`). Keep it as-is. It does not use `BadgeDirective` internally - it has its own CVA. No changes needed.

**Verify**: `ng build` passes.

### 1.3 Input

**Current**: `src/shared/components/form-field/text-input.directive.ts` - CVA directive with `[appTextInput]` selector. Variants: `default`, `error`, `success`. Sizes: `sm`, `md`, `lg`.

**Target**: Spartan Input Helm directive. Also consider replacing the CSS `.input` class in `src/styles.css` with Spartan Input.

**Steps**:
1. Replace directive implementation with Spartan Input.
2. Preserve the `error` and `success` variants (add to Spartan's CVA if not present by default).
3. Update `src/styles.css` - remove the `.input` CSS class definition (used in `header.component.ts` search input). Replace with Spartan Input directive on those elements.

**Verify**: `ng build` passes. Form inputs render correctly with validation states.

### 1.4 Spinner

**Current**: `src/shared/components/spinner/spinner.component.ts` - Custom SVG animation component with sizes `sm`, `md`, `lg`.

**Target**: Spartan Spinner.

**Steps**:
1. Replace implementation with Spartan Spinner Helm.
2. Ensure size mapping is preserved (`sm`/`md`/`lg`).
3. Keep selector as `app-spinner` or adopt Spartan's selector. Re-export from `src/shared/components/spinner/index.ts`.

**Verify**: `ng build` passes. Loading states render correctly.

### 1.5 Icon

**Current**: `src/shared/components/icon/icon.component.ts` - Wraps `MatIconModule` with `name` and `size` inputs. Uses Material Icons font.

**Decision point**: Spartan Icon uses `ng-icons`, while the current codebase uses `lucide-angular` and Material Icons font. Options:
- **Option A**: Switch to `ng-icons` (Spartan's default). Supports Lucide icon set. Requires updating all icon name references from Material names (`menu`, `account_circle`, `expand_more`) to Lucide names (`lucideMenu`, `lucideCircleUser`, `lucideChevronDown`).
- **Option B**: Keep `lucide-angular` directly without a wrapper component. Import `LucideAngularModule` and use `<lucide-icon name="..." />` directly in templates.
- **Option C**: Keep current `IconComponent` but replace Material Icons font with `lucide-angular` internally. Requires icon name mapping.

Recommended: **Option A** (ng-icons with Lucide set) for consistency with Spartan ecosystem. Create a mapping of Material icon names to Lucide equivalents during migration.

**Known icon name mappings needed**:
| Material Name | Lucide Equivalent |
|---|---|
| `menu` | `lucideMenu` |
| `account_circle` | `lucideCircleUser` |
| `expand_more` | `lucideChevronDown` |
| `person` | `lucideUser` |
| `settings` | `lucideSettings` |
| `business` | `lucideBuilding2` |
| `logout` | `lucideLogOut` |
| `light_mode` | `lucideSun` |
| `dark_mode` | `lucideMoon` |
| `arrow_upward` | `lucideArrowUp` |
| `arrow_downward` | `lucideArrowDown` |
| `unfold_more` | `lucideChevronsUpDown` |
| `more_vert` | `lucideMoreVertical` |
| `add` | `lucidePlus` |
| `search` | `lucideSearch` |
| `close` | `lucideX` |
| `refresh` | `lucideRefreshCw` |
| `filter_list` | `lucideFilter` |
| `view_column` | `lucideColumns` |
| `error` | `lucideAlertCircle` |
| `hourglass_empty` | `lucideHourglass` |
| `check` | `lucideCheck` |
| `edit` | `lucidePencil` |
| `delete` | `lucideTrash2` |
| `arrow_back` | `lucideArrowLeft` |

Search for all `<app-icon name="..."` and `name="lucide..."` in templates to build the complete mapping before starting.

**Verify**: `ng build` passes. All icons render correctly.

---

## Phase 2: Migrate Services (Dialog + Toast)

### 2.1 Toast Service (Material Snackbar -> Spartan Sonner)

**Current**: `src/app/core/services/toast.service.ts` uses `MatSnackBar` with methods: `show()`, `success()`, `error()`, `info()`, `warning()`.

**Target**: Spartan Sonner (`ngx-sonner`) which provides `toast()`, `toast.success()`, `toast.error()`, `toast.info()`, `toast.warning()`.

**Steps**:
1. Add the Sonner toaster component to the root `app.component.ts` template (add `<hlm-toaster />` or the Spartan Sonner component).
2. Rewrite `ToastService` to use `toast()` from `ngx-sonner` instead of `MatSnackBar`.
3. The API mapping is straightforward:
   - `this.snackBar.open(message, action, config)` -> `toast(message, { action, duration })`
   - `success(msg)` -> `toast.success(msg)`
   - `error(msg)` -> `toast.error(msg, { duration: 5000 })`
   - `info(msg)` -> `toast.info(msg)`
   - `warning(msg)` -> `toast.warning(msg)`
4. Remove emoji prefixes (`✓`, `✗`, `ℹ`, `⚠`) from toast messages - Sonner handles visual differentiation with icons/styling.
5. Position configuration: Sonner uses a `position` prop on the toaster component (e.g., `position="bottom-right"`) instead of per-toast config.

**Verify**: `ng build` passes. Trigger a toast in the running app (e.g., save an entity) and confirm rendering.

### 2.2 Dialog Service + Confirm Dialog (Material Dialog -> Spartan Alert Dialog)

**Current**:
- `src/app/core/services/dialog.service.ts` - Uses `MatDialog.open()`, returns `Observable<boolean>`.
- `src/shared/components/confirm-dialog/confirm-dialog.component.ts` - Material Dialog component with `MAT_DIALOG_DATA` injection.

**Target**: Spartan Alert Dialog (brain + helm).

**Steps**:
1. Rewrite `ConfirmDialogComponent` using Spartan Alert Dialog Helm components. The Spartan Alert Dialog provides `hlm-alert-dialog`, `hlm-alert-dialog-header`, `hlm-alert-dialog-title`, `hlm-alert-dialog-description`, `hlm-alert-dialog-footer`, `hlm-alert-dialog-action`, `hlm-alert-dialog-cancel` (check exact element names in the generated Helm).
2. Spartan Dialog uses a different opening mechanism than Material's imperative `MatDialog.open()`. The brain provides `BrnAlertDialogTrigger` for template-driven opening, or `BrnDialogService` for programmatic opening. The `DialogService` should be updated to use the programmatic approach.
3. Preserve the public API: `DialogService.confirm(data): Observable<boolean>`.
4. Verify the `ConfirmDialogData` interface (`title`, `description`, `confirmText`, `cancelText`, `variant`) is supported by the new implementation.
5. Remove `MatDialogModule`, `MAT_DIALOG_DATA`, `MatDialogRef`, `MatDialog` imports.

**Consumers of DialogService** (search for `inject(DialogService)`):
- Feature detail components (property-details, company-details, building-details, organization-details) for delete confirmations.

**Verify**: `ng build` passes. Open a delete confirmation dialog to verify rendering and confirm/cancel behavior.

---

## Phase 3: Migrate Interactive Components (Aria -> Spartan Brain)

### 3.1 Tabs (4 feature detail components)

**Current**: Feature detail components import `Tabs`, `TabList`, `Tab`, `TabPanel`, `TabContent` from `@angular/aria/tabs`.

**Target**: Spartan Tabs Helm components.

**Files to update**:
- `src/app/features/properties/details/property-details.component.ts`
- `src/app/features/buildings/details/building-details.component.ts`
- `src/app/features/companies/details/company-details.component.ts`
- `src/app/features/organizations/details/organization-details.component.ts`

**Steps per file**:
1. Replace imports: remove `@angular/aria/tabs`, add Spartan Tabs Helm imports.
2. Update template: replace Aria tab directives with Spartan tab components. The general structure is preserved (tab list -> tabs -> panels) but element names change.
3. Aria Tabs uses directives on native elements (`<div ngTab>`). Spartan Tabs uses components (`<hlm-tab>`). Adjust templates accordingly.
4. Preserve any `class` attributes - Spartan components accept classes via `hlm` directive or pass-through `class` attributes.

**Template pattern change**:
```html
<!-- BEFORE (Angular Aria) -->
<div ngTabs>
  <div ngTabList class="...">
    <button ngTab class="...">Tab 1</button>
    <button ngTab class="...">Tab 2</button>
  </div>
  <div ngTabPanel class="...">Content 1</div>
  <div ngTabPanel class="...">Content 2</div>
</div>

<!-- AFTER (Spartan) - check generated Helm for exact selectors -->
<hlm-tabs defaultValue="tab1">
  <hlm-tabs-list class="...">
    <button hlmTabsTrigger="tab1">Tab 1</button>
    <button hlmTabsTrigger="tab2">Tab 2</button>
  </hlm-tabs-list>
  <div hlmTabsContent="tab1">Content 1</div>
  <div hlmTabsContent="tab2">Content 2</div>
</hlm-tabs>
```

**Verify**: `ng build` passes. Navigate to each feature detail view and verify tab switching works.

### 3.2 Accordion (property details)

**Current**: `property-details.component.ts` imports `AccordionGroup`, `AccordionPanel`, `AccordionTrigger`, `AccordionContent` from `@angular/aria/accordion`.

**Target**: Spartan Accordion Helm components.

**Steps**:
1. Replace imports.
2. Update template to use Spartan Accordion components.
3. Check the template in `property-details.component.html` for exact usage pattern and map to Spartan equivalents.

**Verify**: `ng build` passes. Accordion sections expand/collapse correctly on property details.

### 3.3 Menu (header + entity-list-view)

**Current**:
- `header.component.ts` imports `Menu`, `MenuContent`, `MenuItem`, `MenuTrigger` from `@angular/aria/menu`.
- `entity-list-view.component.ts` imports the same.

Both use CDK Overlay (`cdkConnectedOverlay`) for positioning.

**Target**: Spartan Dropdown Menu Helm.

**Steps**:
1. Replace Aria Menu imports with Spartan Dropdown Menu imports.
2. Spartan Dropdown Menu includes its own overlay/positioning mechanism (brain handles this). Remove `cdkConnectedOverlay` directives from menu templates.
3. Update template patterns:

```html
<!-- BEFORE (Angular Aria + CDK Overlay) -->
<button ngMenuTrigger #trigger="ngMenuTrigger" #origin [menu]="menuRef()">
  Trigger
</button>
<ng-template
  [cdkConnectedOverlayOpen]="trigger.expanded()"
  [cdkConnectedOverlay]="{origin, usePopover: 'inline'}"
  [cdkConnectedOverlayPositions]="[...]"
  cdkAttachPopoverAsChild>
  <div ngMenu #menuRef="ngMenu" class="...">
    <ng-template ngMenuContent>
      <div ngMenuItem value="..." (click)="..." class="...">Item</div>
    </ng-template>
  </div>
</ng-template>

<!-- AFTER (Spartan) - check generated Helm for exact selectors -->
<hlm-menu-dropdown>
  <button hlmMenuTrigger>Trigger</button>
  <hlm-menu>
    <button hlmMenuItem (click)="...">Item</button>
  </hlm-menu>
</hlm-menu-dropdown>
```

4. For the entity-list-view, there are 3 menu instances (`tableMenu`, `actionMenu`, `mobileActionMenu`). Update each one. Also read the entity-list-view HTML template for full context.
5. The header menu uses `cdkAttachPopoverAsChild` for popover-as-child behavior. Verify Spartan handles this correctly or adjust.

**Also update**: Remove `OverlayModule` imports from components where it was used solely for menu positioning. Keep it where it's used for other purposes (select, autocomplete - but those are being replaced too).

**Verify**: `ng build` passes. Test header user menu and entity list row action menus.

### 3.4 Toolbar (entity-list-view)

**Current**: `entity-list-view.component.ts` imports `Toolbar`, `ToolbarWidget` from `@angular/aria/toolbar`.

**Target**: Spartan does not have a Toolbar component. Replace with plain Tailwind-styled `<div>` with appropriate `role="toolbar"` and `aria-label` attributes.

**Steps**:
1. Remove `Toolbar`, `ToolbarWidget` imports.
2. In the template, replace `<div ngToolbar ...>` with `<div role="toolbar" aria-label="..." class="...">`.
3. Remove `ngToolbarWidget` directives from child elements.
4. Keyboard navigation (arrow keys between toolbar items) will need manual implementation if required, or can be deferred. The Aria toolbar primarily provides keyboard focus management within the toolbar.

**Verify**: `ng build` passes. List view toolbar renders and functions correctly.

---

## Phase 4: Migrate Composed Components

### 4.1 Select Component

**Current**: `src/shared/components/select/select.component.ts` (130 lines) - Wraps Aria Combobox + Listbox + CDK Overlay. Provides `app-select` with inputs: `label`, `placeholder`, `value`, `optionsProvider`, output: `valueChange`. Internally manages loading state, option rendering, overlay positioning.

**Target**: Spartan Select Helm.

**Steps**:
1. Study the generated Spartan Select Helm component structure.
2. Rewrite `SelectComponent` using Spartan Select primitives. The public API (`label`, `placeholder`, `value`, `optionsProvider`, `valueChange`) should be preserved to minimize consumer changes.
3. Spartan Select handles overlay positioning internally (via brain), so remove CDK Overlay dependencies.
4. Loading state handling: Spartan Select may not have built-in loading support. If so, keep the `loading` signal and conditionally render a spinner inside the select dropdown.
5. Remove Aria Combobox/Listbox imports.

**Consumers**: `entity-list-view.component.ts` (filter dropdowns), various create-drawer components.

**Verify**: `ng build` passes. Test select dropdowns in list views and create forms.

### 4.2 Autocomplete Component

**Current**: `src/shared/components/autocomplete/autocomplete.component.ts` (125 lines) - Wraps Aria Combobox + Listbox + CDK Overlay. Provides `app-autocomplete` with filtering, keyboard navigation, accessible selection.

**Target**: Spartan Autocomplete/Combobox Helm (check which component maps better).

**Steps**:
1. Study the generated Spartan Autocomplete Helm structure.
2. Rewrite preserving public API: `optionsProvider`, `value`, `placeholder`, `disabled`, `ariaLabel`, `expandOnFocus`, `valueChange`.
3. Preserve client-side filtering behavior (`filteredOptions` computed signal).
4. Remove Aria Combobox/Listbox and CDK Overlay imports.

**Consumers**: Feature create-drawer components (company selector, etc.).

**Verify**: `ng build` passes. Test autocomplete in create flows.

### 4.3 Entity Details View

**Current**: `src/shared/components/entity-details-view/entity-details-view.component.ts` uses CDK `OverlayModule` for action menu overlay.

**Target**: If CDK Overlay is used here only for a menu, replace with Spartan Dropdown Menu (already migrated in Phase 3). If used for drawer/side panel behavior, replace with Spartan Sheet.

**Steps**:
1. Read the full entity-details-view template to understand CDK Overlay usage.
2. Replace with appropriate Spartan component.
3. Remove `OverlayModule` import if no longer needed.

**Verify**: `ng build` passes. Entity detail views render correctly with action menus/drawers.

### 4.4 Entity List View

**Current**: `src/shared/components/entity-list-view/entity-list-view.component.ts` (404 lines) imports Aria Menu + Toolbar + CDK Overlay.

After Phases 3.3, 3.4, and 4.1 (Menu, Toolbar, Select migrations), this file's Aria/CDK imports should already be replaced. Verify:
1. No remaining `@angular/aria/*` imports.
2. CDK Overlay import is removed (menu positioning now handled by Spartan).
3. CDK Layout import may still be needed via `ResponsiveService` - that's fine, it stays.

**Verify**: `ng build` passes. Full list view functionality works (sort, filter, paginate, row actions).

---

## Phase 5: Migrate Styles

### 5.1 Update `src/styles.css`

**Remove**:
- `@import '@angular/cdk/overlay-prebuilt.css';` (included in Spartan preset)
- Material bridge variables:
  ```css
  :root {
    --mat-sys-on-surface: var(--color-foreground);
    --mat-mdc-dialog-container-bg: var(--color-card);
  }
  ```
- Material override classes:
  ```css
  .toast-panel { ... }
  .dialog-panel { ... }
  .mat-mdc-dialog-surface { background-color: transparent !important; }
  ```
- CSS utility classes that are now handled by Spartan components: `.card`, `.card-header`, `.card-title`, `.card-description`, `.card-content`, `.card-footer`, `.input`, `.badge`, etc. (check each one for usage before removing; some may be used directly in templates without a component)

**Add**:
- `@import "@spartan-ng/brain/hlm-tailwind-preset.css";`

**Preserve**:
- `@import "tailwindcss";`
- `@theme { ... }` block with color variables (update format if switching to OKLCH)
- `.dark { ... }` theme overrides
- `.btn`, `.btn-primary` etc. - Remove if all button usages have switched to Spartan Button directive. If some standalone HTML uses these classes, keep them temporarily.
- `.listbox`, `.listbox-option` - Remove if all select/autocomplete usages switched to Spartan.

**Approach**: Remove CSS classes one at a time. After removing each class, search for its usage in templates (`class="card"`, `class="card-header"`, etc.). Replace template usages with Spartan component equivalents or explicit Tailwind classes.

### 5.2 Update Drawer Component

**Current**: `src/shared/components/drawer/` has only a `README.md` placeholder.

**Target**: Spartan Sheet is the drawer replacement. The create-drawer components (`create-company-drawer`, `create-property-drawer`, `create-building-drawer`) should use Spartan Sheet for side-panel behavior.

**Steps**:
1. Implement the drawer using Spartan Sheet Helm.
2. Update create-drawer components to use it.

---

## Phase 6: Remove Old Dependencies

### 6.1 Remove `@angular/material`

1. Search the entire codebase for `@angular/material` imports. There should be zero remaining.
   ```bash
   grep -r "@angular/material" src/
   ```
2. If any remain, migrate them first.
3. Remove from `package.json`:
   ```bash
   npm uninstall @angular/material
   ```

### 6.2 Remove `@angular/aria`

1. Search for remaining Aria imports:
   ```bash
   grep -r "@angular/aria" src/
   ```
2. If any remain, migrate them.
3. Remove from `package.json`:
   ```bash
   npm uninstall @angular/aria
   ```

### 6.3 Evaluate `@angular/animations`

`@angular/animations` was required by Material. Check if Spartan or any other dependency needs it:
```bash
grep -r "@angular/animations" src/
grep -r "provideAnimations\|BrowserAnimationsModule" src/
```
If only Material used it, remove it:
```bash
npm uninstall @angular/animations
```

Also remove any `provideAnimations()` or `provideAnimationsAsync()` from the app config if present.

### 6.4 Clean package-lock and verify

```bash
rm -rf node_modules package-lock.json
npm install
npx ng build
```

---

## Phase 7: Update Documentation and Tooling

### 7.1 Update Angular Instructions

Update `.github/instructions/angular.instructions.md`:

Replace the "UI Component Strategy" section:
```markdown
## UI Component Strategy (priority order)
1. **Spartan UI** (brain + helm) - All interactive components: Select, Autocomplete, Menu, Tabs, Accordion, Dialog, Sheet, Toast, etc.
2. **Spartan Primitives** - Button, Badge, Input, Card, Label, Separator via Helm directives.
3. **Angular CDK** - Drag/drop, Virtual scroll, Clipboard, Platform detection, BreakpointObserver.
4. **Pure Tailwind** - Simple layouts, spacing, grids. No component-level CSS classes.

Spartan Helm components live in `shared/components/ui/` and are owned code. Modify directly.
Do NOT wrap Spartan components in pass-through wrappers. Import and use directly.
```

### 7.2 Update Angular Feature Structure Doc

Update `docs/dev/angular-feature-structure.md`:
- Update the shared components table to reflect Spartan replacements.
- Add `shared/components/ui/` directory to the structure diagram.
- Update component import examples.

### 7.3 Update New Angular Feature Skill

Update `.github/skills/new-angular-feature/SKILL.md`:
- Replace Aria import examples with Spartan imports.
- Update template patterns (Tabs, Menu, Dialog examples).
- Update scaffolding instructions for new components.

### 7.4 Update ADR 0011

ADR 0011 references Angular Aria and Material directly. Add a note that it has been superseded by ADR 0016 for component library choices, though the UX patterns (TanStack Table, auto-save, filter panel, etc.) remain valid.

---

## Verification Checklist

Run after all phases are complete:

- [ ] `ng build` succeeds with zero errors
- [ ] `ng test` (vitest) passes
- [ ] `grep -r "@angular/material" src/` returns zero results
- [ ] `grep -r "@angular/aria" src/` returns zero results
- [ ] `@angular/material` is NOT in `package.json`
- [ ] `@angular/aria` is NOT in `package.json`
- [ ] `@spartan-ng/brain` IS in `package.json` dependencies
- [ ] Companies: list view renders, detail view tabs work, create drawer opens, delete confirm dialog works, toast appears on save
- [ ] Properties: list view renders, detail view tabs + accordion work, create drawer opens
- [ ] Buildings: list view renders, detail view tabs work, create drawer opens
- [ ] Organizations: detail view tabs work
- [ ] Header: user menu opens, all menu items clickable, theme toggle works, language selector works
- [ ] Dark mode: toggle and verify all components respect dark theme
- [ ] Mobile: responsive layout works, burger menu works, list views show card layout
- [ ] Accessibility: run AXE on at least one list view and one detail view
- [ ] Bundle size: compare `ng build` output stats before and after migration. Material + Aria removal should roughly offset Spartan Brain addition
