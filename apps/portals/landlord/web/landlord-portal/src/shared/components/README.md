# Shared Components

This directory contains reusable UI primitives and business components following the Pure Angular Component Strategy (ADR 0012).

## Architecture

We use a 4-layer architecture:

### Layer 1: Primitives (CVA Directives)
Directives that apply variant styles to native HTML elements using `class-variance-authority`.

**Location**: `/shared/components/{component}/`

**Examples**:
- `[appBtn]` - Button variants
- `[appBadge]` - Badge variants

**Pattern**:
```typescript
@Directive({
  selector: '[appBtn]',
  host: { '[class]': 'classes()' }
})
export class ButtonDirective {
  variant = input<'default' | 'destructive' | 'outline'>('default');
  size = input<'default' | 'sm' | 'lg'>('default');
  classes = computed(() => buttonVariants({ variant: this.variant(), size: this.size() }));
}
```

**Usage**:
```html
<button appBtn variant="destructive" size="sm">Delete</button>
<span appBadge variant="outline">New</span>
```

### Layer 2: Interactive Components (Direct Usage)
Use Angular Aria directives directly in templates without creating wrapper components.

**Pattern**: Import and use directives directly
```html
<!-- Menu -->
<button [ngMenuTrigger]="menu">Actions</button>
<ng-template #menu>
  <div ngMenu>
    <button ngMenuItem (click)="edit()">Edit</button>
    <button ngMenuItem (click)="delete()">Delete</button>
  </div>
</ng-template>

<!-- Dialog -->
<button [ngDialogTrigger]="dialog">Open</button>
<ng-template #dialog>
  <div ngDialog>
    <h2>Dialog Title</h2>
    <p>Dialog content</p>
  </div>
</ng-template>
```

**Anti-Pattern**: ❌ Don't create wrappers
```html
<!-- DON'T DO THIS -->
<app-menu [items]="menuItems" />
<app-dialog [title]="title" [content]="content" />
```

### Layer 3: Complex Components (Direct Usage)
Use Angular Material directly for datepicker and slider.

**Pattern**: Import Material components and use directly
```html
<mat-form-field>
  <mat-label>Date</mat-label>
  <input matInput [matDatepicker]="picker" [formControl]="date">
  <mat-datepicker-toggle matSuffix [for]="picker" />
  <mat-datepicker #picker />
</mat-form-field>
```

**Anti-Pattern**: ❌ Don't create wrappers
```html
<!-- DON'T DO THIS -->
<app-datepicker [control]="date" label="Date" />
```

### Layer 4: Business Components
Custom components that add domain-specific business logic.

**Location**: `/shared/components/{component}/`

**When to Create**:
1. Used in 10+ places
2. Adds business logic (not just styling or proxying)
3. Reduces duplication of domain patterns
4. Provides consistent domain behavior

**Example**:
```typescript
// property-table.component.ts - Adds business logic
@Component({
  selector: 'app-property-table',
  template: `
    <tanstack-table [data]="data()" [columns]="propertyColumns">
      <!-- Custom: Property-specific columns, filters, validation -->
    </tanstack-table>
  `
})
export class PropertyTableComponent {
  data = input.required<Property[]>();
  
  // Business logic: Property-specific columns with domain validation
  propertyColumns = [
    { id: 'name', header: 'Property', cell: /* custom */ },
    { id: 'units', header: 'Units', cell: /* with validation */ },
  ];
}
```

## Directory Structure

```
shared/components/
├── button/
│   ├── button.directive.ts       # Layer 1: CVA primitive
│   └── index.ts
├── badge/
│   ├── badge.directive.ts        # Layer 1: CVA primitive
│   └── index.ts
├── property-table/               # Layer 4: Business component
│   ├── property-table.component.ts
│   ├── property-table.component.html
│   └── index.ts
└── README.md (this file)
```

## Usage Examples

### Buttons (Layer 1: CVA Directive)
```html
<!-- Primary button -->
<button appBtn>Save</button>

<!-- Destructive action -->
<button appBtn variant="destructive">Delete</button>

<!-- Outline style -->
<button appBtn variant="outline" size="sm">Cancel</button>

<!-- Ghost button -->
<button appBtn variant="ghost">Learn More</button>

<!-- Icon button -->
<button appBtn variant="ghost" size="icon">
  <ng-icon name="lucideTrash" />
</button>
```

### Badges (Layer 1: CVA Directive)
```html
<!-- Default badge -->
<span appBadge>Active</span>

<!-- Status badges -->
<span appBadge variant="secondary">Draft</span>
<span appBadge variant="destructive">Overdue</span>
<span appBadge variant="outline">Pending</span>
```

### Menu (Layer 2: Angular Aria Direct)
```html
<button [ngMenuTrigger]="actions">
  <ng-icon name="lucideMoreVertical" />
</button>

<ng-template #actions>
  <div ngMenu class="min-w-[200px] rounded-md border bg-popover p-1 shadow-md">
    <button ngMenuItem class="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm hover:bg-accent">
      <ng-icon name="lucideEdit" />
      Edit
    </button>
    <button ngMenuItem class="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm hover:bg-accent">
      <ng-icon name="lucideTrash" />
      Delete
    </button>
  </div>
</ng-template>
```

### Datepicker (Layer 3: Material Direct)
```html
<mat-form-field>
  <mat-label>Start Date</mat-label>
  <input matInput [matDatepicker]="startPicker" [formControl]="startDate">
  <mat-datepicker-toggle matSuffix [for]="startPicker" />
  <mat-datepicker #startPicker />
</mat-form-field>
```

## Anti-Patterns

### ❌ Don't Create Proxy Wrappers
```typescript
// BAD: Just proxying inputs without adding value
@Component({
  selector: 'app-menu',
  template: `<div ngMenu>...</div>`
})
export class MenuComponent {
  items = input<MenuItem[]>();  // Just proxying
}
```

### ❌ Don't Create "Switchable" Abstractions
```typescript
// BAD: False portability claim
@Component({ selector: 'app-datepicker' })
export class DatepickerComponent {
  // Switching libraries requires rewriting usages anyway
}
```

### ❌ Don't Wrap Without Business Logic
```typescript
// BAD: No business logic added
@Component({ selector: 'app-table' })
export class TableComponent {
  data = input<any[]>();        // Just proxying
  columns = input<Column[]>();  // Just proxying
}
```

### ✅ DO Create Business Components
```typescript
// GOOD: Adds business value
@Component({ selector: 'app-tenant-selector' })
export class TenantSelectorComponent {
  // Adds: Multi-tenant filtering, org hierarchy, permission checks
  // Wraps: ngSelect + domain logic
  // Justification: Used 15+ places with consistent behavior
}
```

## Import Patterns

```typescript
// Import CVA directives
import { ButtonDirective } from '@/shared/components/button';
import { BadgeDirective } from '@/shared/components/badge';

// Import Angular Aria (use directly)
import { NgMenu, NgMenuItem, NgMenuTrigger } from '@angular/aria/menu';

// Import Material (use directly)
import { MatDatepicker, MatDatepickerInput, MatDatepickerToggle } from '@angular/material/datepicker';

// Import business components
import { PropertyTableComponent } from '@/shared/components/property-table';
```

## References

- [ADR 0012: Pure Angular Component Strategy](/docs/decisions/0012-headless-ui-component-strategy.md)
- [Angular Aria Documentation](https://angular.dev/guide/aria)
- [Angular Material Documentation](https://material.angular.io)
- [class-variance-authority](https://cva.style/docs)
