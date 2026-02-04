# Migration Guide: Material to Headless Components

This guide documents the migration from Angular Material to our new headless component stack (Angular Aria + Spartan + TanStack Table).

## Overview

We are replacing Angular Material with:
- **Angular Aria**: Official Angular headless components
- **Spartan UI**: shadcn-inspired components for Angular
- **TanStack Table**: Headless data table
- **Tailwind CSS**: All styling (no component library CSS)

See [ADR 0012](../decisions/0012-headless-ui-component-strategy.md) for the full rationale.

## Step 1: Install Dependencies

```bash
cd apps/portals/landlord/web/landlord-portal

# Remove Material (optional - can do after migration)
npm uninstall @angular/material

# Install new dependencies
npm install @angular/aria @angular/cdk
npm install @spartan-ng/brain
npm install @tanstack/angular-table
npm install -D @spartan-ng/cli

# Initialize Spartan components (generates local component files)
npx ng g @spartan-ng/cli:ui button
npx ng g @spartan-ng/cli:ui dialog
npx ng g @spartan-ng/cli:ui input
# ... add more as needed
```

## Step 2: Configure Tailwind

### Remove Material Theme

Delete `src/theme.css` (Material theme file).

### Update `src/styles.css`

Tailwind v4 uses CSS-first configuration with `@theme`. No `tailwind.config.js` needed.

```css
@import "tailwindcss";

/* Tailwind v4: Define design tokens with @theme */
@theme {
  /* Colors using HSL values */
  --color-background: hsl(0 0% 100%);
  --color-foreground: hsl(222.2 84% 4.9%);
  --color-card: hsl(0 0% 100%);
  --color-card-foreground: hsl(222.2 84% 4.9%);
  --color-popover: hsl(0 0% 100%);
  --color-popover-foreground: hsl(222.2 84% 4.9%);
  --color-primary: hsl(222.2 47.4% 11.2%);
  --color-primary-foreground: hsl(210 40% 98%);
  --color-secondary: hsl(210 40% 96.1%);
  --color-secondary-foreground: hsl(222.2 47.4% 11.2%);
  --color-muted: hsl(210 40% 96.1%);
  --color-muted-foreground: hsl(215.4 16.3% 46.9%);
  --color-accent: hsl(210 40% 96.1%);
  --color-accent-foreground: hsl(222.2 47.4% 11.2%);
  --color-destructive: hsl(0 84.2% 60.2%);
  --color-destructive-foreground: hsl(210 40% 98%);
  --color-border: hsl(214.3 31.8% 91.4%);
  --color-input: hsl(214.3 31.8% 91.4%);
  --color-ring: hsl(222.2 84% 4.9%);

  /* Border radius */
  --radius-sm: 0.25rem;
  --radius-md: 0.5rem;
  --radius-lg: 0.75rem;
}

/* Dark mode overrides */
@layer base {
  .dark {
    --color-background: hsl(222.2 84% 4.9%);
    --color-foreground: hsl(210 40% 98%);
    --color-card: hsl(222.2 84% 4.9%);
    --color-card-foreground: hsl(210 40% 98%);
    --color-popover: hsl(222.2 84% 4.9%);
    --color-popover-foreground: hsl(210 40% 98%);
    --color-primary: hsl(210 40% 98%);
    --color-primary-foreground: hsl(222.2 47.4% 11.2%);
    --color-secondary: hsl(217.2 32.6% 17.5%);
    --color-secondary-foreground: hsl(210 40% 98%);
    --color-muted: hsl(217.2 32.6% 17.5%);
    --color-muted-foreground: hsl(215 20.2% 65.1%);
    --color-accent: hsl(217.2 32.6% 17.5%);
    --color-accent-foreground: hsl(210 40% 98%);
    --color-destructive: hsl(0 62.8% 30.6%);
    --color-destructive-foreground: hsl(210 40% 98%);
    --color-border: hsl(217.2 32.6% 17.5%);
    --color-input: hsl(217.2 32.6% 17.5%);
    --color-ring: hsl(212.7 26.8% 83.9%);
  }
}
```

Now you can use these colors directly in Tailwind classes: `bg-primary`, `text-foreground`, `border-border`, etc.

### Reusable Component Classes with `@layer components`

When building custom components (especially with Angular CDK), use `@layer components` to avoid repeating long utility strings:

```css
/* Add after @theme and @layer base in styles.css */
@layer components {
  /* Buttons */
  .btn {
    @apply inline-flex items-center justify-center gap-2 rounded-md px-4 py-2
           text-sm font-medium transition-colors focus-visible:outline-none
           focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none
           disabled:opacity-50;
  }
  .btn-primary {
    @apply bg-primary text-primary-foreground hover:bg-primary/90;
  }
  .btn-secondary {
    @apply bg-secondary text-secondary-foreground hover:bg-secondary/80;
  }
  .btn-ghost {
    @apply hover:bg-accent hover:text-accent-foreground;
  }
  .btn-destructive {
    @apply bg-destructive text-destructive-foreground hover:bg-destructive/90;
  }

  /* Inputs */
  .input {
    @apply flex h-10 w-full rounded-md border border-input bg-background px-3 py-2
           text-sm ring-offset-background file:border-0 file:bg-transparent
           file:text-sm file:font-medium placeholder:text-muted-foreground
           focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring
           disabled:cursor-not-allowed disabled:opacity-50;
  }

  /* Cards */
  .card {
    @apply rounded-lg border bg-card text-card-foreground shadow-sm;
  }
  .card-header {
    @apply flex flex-col space-y-1.5 p-6;
  }
  .card-title {
    @apply text-2xl font-semibold leading-none tracking-tight;
  }
  .card-description {
    @apply text-sm text-muted-foreground;
  }
  .card-content {
    @apply p-6 pt-0;
  }
  .card-footer {
    @apply flex items-center p-6 pt-0;
  }

  /* Listbox/Select (for Angular CDK/Aria) */
  .listbox {
    @apply rounded-md border bg-popover p-1 text-popover-foreground shadow-md;
  }
  .listbox-option {
    @apply relative flex cursor-pointer select-none items-center rounded-sm
           px-2 py-1.5 text-sm outline-none transition-colors
           hover:bg-accent hover:text-accent-foreground
           data-[disabled]:pointer-events-none data-[disabled]:opacity-50;
  }
  .listbox-option-selected {
    @apply bg-accent text-accent-foreground;
  }

  /* Badge */
  .badge {
    @apply inline-flex items-center rounded-full border px-2.5 py-0.5
           text-xs font-semibold transition-colors;
  }
  .badge-primary {
    @apply border-transparent bg-primary text-primary-foreground;
  }
  .badge-secondary {
    @apply border-transparent bg-secondary text-secondary-foreground;
  }
  .badge-destructive {
    @apply border-transparent bg-destructive text-destructive-foreground;
  }
  .badge-outline {
    @apply text-foreground;
  }
}
```

**Usage in templates:**
```html
<!-- Simple usage -->
<button class="btn btn-primary">Save</button>

<!-- With additional utilities (they override component classes) -->
<button class="btn btn-secondary px-8">Wider Button</button>

<!-- CDK Listbox with component classes -->
<div cdkListbox class="listbox">
  <div cdkOption class="listbox-option">Option 1</div>
</div>
```

### No `tailwind.config.js` Needed

Tailwind v4 is CSS-first. The `@theme` block above replaces the JS config file. If you have an existing `tailwind.config.js`, you can delete it.

The project already has `.postcssrc.json` configured with `@tailwindcss/postcss`.

### Update `index.html`

Remove Material Icons font (optional - keep if using material icons):
```html
<!-- Remove if not using Material Icons -->
<link href="https://fonts.googleapis.com/icon?family=Material+Icons" rel="stylesheet">
```

Consider using Lucide icons instead (what Spartan/shadcn uses):
```bash
npm install lucide-angular
```

## Step 3: Component Migration

### Services to KEEP

The following services are framework-agnostic and should be preserved:
- `ResponsiveService` - Breakpoint detection
- `SessionService` - Authentication state
- `HealthService` - Backend health monitoring
- `UserPreferencesService` - Theme, language preferences

### Components to REBUILD

All layout and UI components should be rebuilt from scratch using the new stack.

### Migration Examples

#### Button (Material -> Spartan)

**Before (Material):**
```typescript
import { MatButtonModule } from '@angular/material/button';

@Component({
  imports: [MatButtonModule],
  template: `<button mat-raised-button color="primary">Click</button>`
})
```

**After (Spartan):**
```typescript
import { HlmButtonDirective } from '@spartan-ng/ui-button-helm';

@Component({
  imports: [HlmButtonDirective],
  template: `<button hlmBtn variant="default">Click</button>`
})
```

**Or pure Tailwind:**
```typescript
@Component({
  template: `
    <button class="inline-flex items-center justify-center rounded-md bg-primary
                   px-4 py-2 text-sm font-medium text-primary-foreground
                   hover:bg-primary/90 focus-visible:outline-none
                   focus-visible:ring-2 focus-visible:ring-ring">
      Click
    </button>
  `
})
```

#### Select (Material -> Angular Aria)

**Before (Material):**
```typescript
import { MatSelectModule } from '@angular/material/select';

@Component({
  imports: [MatSelectModule],
  template: `
    <mat-select [(value)]="selected">
      <mat-option value="1">Option 1</mat-option>
    </mat-select>
  `
})
```

**After (Angular Aria):**
```typescript
import { CdkListbox, CdkOption } from '@angular/cdk/listbox';
// Or use @angular/aria when available

@Component({
  imports: [CdkListbox, CdkOption],
  template: `
    <div cdkListbox [(ngModel)]="selected" class="...tailwind classes...">
      <div cdkOption="1" class="...">Option 1</div>
    </div>
  `
})
```

#### Dialog (Material -> Spartan)

**Before (Material):**
```typescript
import { MatDialog } from '@angular/material/dialog';

constructor(private dialog: MatDialog) {}

openDialog() {
  this.dialog.open(MyDialogComponent);
}
```

**After (Spartan):**
```typescript
import { BrnDialogTriggerDirective } from '@spartan-ng/brain/dialog';
import { HlmDialogComponent } from '@spartan-ng/ui-dialog-helm';

@Component({
  imports: [BrnDialogTriggerDirective, HlmDialogComponent],
  template: `
    <hlm-dialog>
      <button brnDialogTrigger hlmBtn>Open</button>
      <hlm-dialog-content>
        <hlm-dialog-header>
          <hlm-dialog-title>Title</hlm-dialog-title>
        </hlm-dialog-header>
        <!-- content -->
      </hlm-dialog-content>
    </hlm-dialog>
  `
})
```

#### Data Table (Material -> TanStack + Spartan)

**Before (Material):**
```typescript
import { MatTableModule } from '@angular/material/table';

@Component({
  imports: [MatTableModule],
  template: `
    <mat-table [dataSource]="data">
      <ng-container matColumnDef="name">
        <mat-header-cell *matHeaderCellDef>Name</mat-header-cell>
        <mat-cell *matCellDef="let row">{{ row.name }}</mat-cell>
      </ng-container>
    </mat-table>
  `
})
```

**After (TanStack Table):**
```typescript
import { createAngularTable, getCoreRowModel, FlexRenderDirective } from '@tanstack/angular-table';

@Component({
  imports: [FlexRenderDirective],
  template: `
    <table class="w-full">
      <thead>
        @for (headerGroup of table.getHeaderGroups(); track headerGroup.id) {
          <tr>
            @for (header of headerGroup.headers; track header.id) {
              <th class="px-4 py-2 text-left">
                <ng-container *flexRender="header.column.columnDef.header; props: header.getContext(); let headerText">
                  {{ headerText }}
                </ng-container>
              </th>
            }
          </tr>
        }
      </thead>
      <tbody>
        @for (row of table.getRowModel().rows; track row.id) {
          <tr class="border-b">
            @for (cell of row.getVisibleCells(); track cell.id) {
              <td class="px-4 py-2">
                <ng-container *flexRender="cell.column.columnDef.cell; props: cell.getContext(); let cellText">
                  {{ cellText }}
                </ng-container>
              </td>
            }
          </tr>
        }
      </tbody>
    </table>
  `
})
export class MyTableComponent {
  data = signal<Person[]>([]);

  columns: ColumnDef<Person>[] = [
    { accessorKey: 'name', header: 'Name' },
    { accessorKey: 'email', header: 'Email' },
  ];

  table = createAngularTable(() => ({
    data: this.data(),
    columns: this.columns,
    getCoreRowModel: getCoreRowModel(),
  }));
}
```

## Step 4: Layout Rebuild

The app shell, header, navigation, and footer should be rebuilt using:
- Pure Tailwind for layout (flex, grid)
- Angular CDK for overlay/sidenav behavior if needed
- Spartan Sheet component for mobile drawer

Example minimal layout structure:

```typescript
@Component({
  selector: 'app-shell',
  template: `
    <div class="flex h-screen flex-col">
      <header class="border-b bg-background px-4 py-3">
        <!-- header content -->
      </header>

      <div class="flex flex-1 overflow-hidden">
        <!-- Desktop sidebar -->
        @if (!responsive.isMobile()) {
          <aside class="w-64 border-r bg-background" [class.w-16]="collapsed()">
            <!-- navigation -->
          </aside>
        }

        <main class="flex-1 overflow-auto bg-muted/40 p-6">
          <router-outlet />
        </main>
      </div>

      <footer class="border-t bg-background px-4 py-2">
        <!-- footer content -->
      </footer>
    </div>
  `
})
```

## Step 5: Cleanup

After migration is complete:

```bash
# Remove Material
npm uninstall @angular/material

# Remove any Material-related files
rm src/theme.css  # if still exists with Material imports
```

## Component Decision Quick Reference

When you need a component:

1. **Check Angular Aria first**: Select, Autocomplete, Menu, Tabs, Tree, Accordion, Listbox, Grid, Toolbar
2. **Use Spartan for**: Dialog, Sheet, Date Picker, Toast, Popover, Data Table, Form Field
3. **Pure Tailwind for**: Button, Input, Badge, Card, simple layouts
4. **Angular CDK for**: Drag/drop, Virtual scroll, Clipboard, Platform detection

## Resources

- [Angular Aria Docs](https://angular.dev/guide/aria/overview)
- [Spartan UI Docs](https://spartan.ng)
- [TanStack Table Angular](https://tanstack.com/table/latest/docs/framework/angular/angular-table)
- [Tailwind CSS](https://tailwindcss.com)
- [Lucide Icons](https://lucide.dev)
