# ADR 0012: Pure Angular Component Strategy

**Status**: Accepted
**Date**: 2026-02-05
**Deciders**: Team

## Context
We evaluated three architectural approaches for UI components:
* **Option A: Spartan (Shadcn wrapper)**: Community library that wraps Angular Aria/CDK with pre-styled components.
* **Option B: Custom Wrappers**: Create our own wrapper components around Angular Aria/CDK/Material.
* **Option C: Pure Angular Stack**: Use Angular Aria/CDK/Material directly with CVA for primitives.

### Problems with Spartan
* Adds 30KB+ bundle size for wrapper abstractions
* Creates dependency on community maintenance (not official Angular)
* "Proxy Hell": Wraps the exact same libraries we'd use directly (Aria/CDK)
* Doesn't reduce API complexity (still need to learn Aria/CDK underneath)
* Generated components with Helm CLI add indirection without functional value

### Problems with Custom Wrappers
* "Pass-Through Hell": Creating `<app-menu>` that just proxies `ngMenu` inputs/outputs
* Maintenance burden: Every Angular Aria update requires wrapper updates
* No abstraction value: Wrappers don't simplify the API or add business logic
* False portability: Switching from Aria to another library requires rewriting ALL usages regardless

## Decision
We adopt **Option C: Pure Angular Stack** with a 4-layer architecture:

### 1. Technology Stack
* **Interactive Components**: `@angular/aria` (Menu, Select, Tabs, Dialog, etc.) - used directly
* **Utilities**: `@angular/cdk` (Overlay, Portal, A11y, Drag/Drop, Virtual Scroll)
* **Complex Components**: `@angular/material` (Datepicker, Slider only) - used directly
* **Data Tables**: `@tanstack/angular-table` (Logic engine) - used directly
* **Primitive Styling**: `class-variance-authority` (CVA) - for button/badge/card variants
* **Design System**: Tailwind CSS v4 (Master theme)

### 2. Four-Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ Layer 4: Business Components                                │
│ Location: /features/**/*.component.ts                       │
│ Examples: property-table, tenant-selector, role-badge       │
│ Rule: Only create when adding domain logic (10+ usages)     │
└─────────────────────────────────────────────────────────────┘
                              ▲
┌─────────────────────────────────────────────────────────────┐
│ Layer 3: Complex Components (Direct Usage)                  │
│ Location: Feature templates                                 │
│ Examples: mat-datepicker, mat-slider                        │
│ Rule: Import and use directly, NO wrappers                  │
└─────────────────────────────────────────────────────────────┘
                              ▲
┌─────────────────────────────────────────────────────────────┐
│ Layer 2: Interactive Components (Direct Usage)              │
│ Location: Feature templates                                 │
│ Examples: ngMenuItem, ngMenu, ngMenuTrigger                 │
│ Rule: Import and use directly, NO wrappers                  │
└─────────────────────────────────────────────────────────────┘
                              ▲
┌─────────────────────────────────────────────────────────────┐
│ Layer 1: Primitives (CVA Directives)                        │
│ Location: /shared/components/                               │
│ Examples: [appBtn], [appBadge], [appCard]                   │
│ Rule: Directives that apply CVA variant styles              │
└─────────────────────────────────────────────────────────────┘
```

### 3. Implementation Patterns

#### Layer 1: Primitives (CVA Directives)
Create reusable directives for native HTML elements with variant management:

```typescript
// shared/components/button/button.directive.ts
import { Directive, input, HostBinding } from '@angular/core';
import { cva, type VariantProps } from 'class-variance-authority';

const buttonVariants = cva(
  'inline-flex items-center justify-center rounded-md text-sm font-medium transition-colors focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      variant: {
        default: 'bg-primary text-primary-foreground hover:bg-primary/90',
        destructive: 'bg-destructive text-destructive-foreground hover:bg-destructive/90',
        outline: 'border border-input bg-background hover:bg-accent hover:text-accent-foreground',
        ghost: 'hover:bg-accent hover:text-accent-foreground',
      },
      size: {
        default: 'h-10 px-4 py-2',
        sm: 'h-9 rounded-md px-3',
        lg: 'h-11 rounded-md px-8',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  }
);

@Directive({ selector: '[appBtn]' })
export class ButtonDirective {
  variant = input<VariantProps<typeof buttonVariants>['variant']>('default');
  size = input<VariantProps<typeof buttonVariants>['size']>('default');

  @HostBinding('class') get classes() {
    return buttonVariants({ variant: this.variant(), size: this.size() });
  }
}

// Usage in templates:
// <button appBtn variant="destructive">Delete</button>
// <button appBtn size="sm">Small Button</button>
```

#### Layer 2: Interactive Components (Direct Usage)
Use Angular Aria directives directly in templates without wrappers:

```html
<!-- DO: Use Angular Aria directly -->
<button type="button" [ngMenuTrigger]="userMenu">
  <ng-icon name="lucideUser" />
  Profile
</button>

<ng-template #userMenu>
  <div ngMenu>
    @for (item of menuItems; track item.id) {
      <button ngMenuItem (click)="item.action()">
        <ng-icon [name]="item.icon" />
        {{ item.label }}
      </button>
    }
  </div>
</ng-template>

<!-- DON'T: Create wrapper components -->
<!-- <app-menu [items]="menuItems" /> ❌ ANTI-PATTERN -->
```

#### Layer 3: Complex Components (Direct Usage)
Use Angular Material directly for datepicker/slider:

```html
<!-- DO: Use Material directly -->
<mat-form-field>
  <mat-label>Start Date</mat-label>
  <input matInput [matDatepicker]="picker" [formControl]="startDate">
  <mat-datepicker-toggle matSuffix [for]="picker"></mat-datepicker-toggle>
  <mat-datepicker #picker></mat-datepicker>
</mat-form-field>

<!-- DON'T: Create wrapper -->
<!-- <app-datepicker [control]="startDate" /> ❌ ANTI-PATTERN -->
```

#### Layer 4: Business Components (When Needed)
Only create custom components when adding business logic (10+ usages with shared behavior):

```typescript
// shared/components/property-table/property-table.component.ts
@Component({
  selector: 'app-property-table',
  template: `
    <div class="property-table">
      <tanstack-table [data]="data()" [columns]="propertyColumns">
        <!-- Custom business logic: property-specific columns, filters, actions -->
      </tanstack-table>
    </div>
  `
})
export class PropertyTableComponent {
  data = input.required<Property[]>();

  propertyColumns = [
    { id: 'name', header: 'Property Name', cell: /* custom renderer */ },
    { id: 'units', header: 'Units', cell: /* with business validation */ },
    // ... property-specific columns
  ];
}

// Only create this when:
// 1. Used in 10+ places
// 2. Adds business logic (not just proxying inputs)
// 3. Reduces duplication of domain patterns
```

### 4. File Structure

```
shared/
  components/
    button/
      button.directive.ts         # CVA directive
      button.directive.spec.ts
      index.ts
    badge/
      badge.directive.ts
      index.ts
    card/
      card.directive.ts
      index.ts
    property-table/               # Business component
      property-table.component.ts
      property-table.component.spec.ts
      index.ts
    README.md                     # Pattern documentation
```

### 5. Anti-Patterns (What NOT to Do)

❌ **Don't create wrapper components that just proxy inputs:**
```typescript
// BAD: This adds zero value
@Component({
  selector: 'app-menu',
  template: `<div ngMenu>...</div>`
})
export class MenuComponent {
  items = input<MenuItem[]>();  // Just proxying to ngMenu
  // No business logic added
}
```

❌ **Don't create "switchable" abstractions:**
```typescript
// BAD: False portability claim
@Component({ selector: 'app-datepicker' })
export class DatepickerComponent {
  // Switching from Material to another library will require
  // rewriting ALL feature usages anyway (different APIs)
}
```

❌ **Don't wrap Material components:**
```typescript
// BAD: Pass-through hell
@Component({
  selector: 'app-datepicker',
  template: `<mat-datepicker [startDate]="startDate" [minDate]="minDate" ... />`
})
export class DatepickerWrapperComponent {
  startDate = input<Date>();    // Proxying
  minDate = input<Date>();      // Proxying
  maxDate = input<Date>();      // Proxying
  // ... 20+ more proxied inputs with zero added value
}
```

✅ **DO create business components when adding domain logic:**
```typescript
// GOOD: Adds business value
@Component({ selector: 'app-tenant-selector' })
export class TenantSelectorComponent {
  // Adds: multi-tenant filtering, organization hierarchy, permission checks
  // Wraps: ngSelect + custom business logic
  // Justification: Used in 15+ places with consistent behavior
}
```

### 6. CSS Integration Strategy

Material components use CSS variables from Tailwind theme:

```css
/* styles.css */
@import '@angular/material/prebuilt-themes/indigo-pink.css';

:root {
  /* Map Material tokens to Tailwind variables */
  --mat-sys-primary: theme('colors.primary.DEFAULT');
  --mat-sys-on-primary: theme('colors.primary.foreground');
  --mat-sys-surface: theme('colors.background');
}
```

## Consequences

### Positive
* **Zero Wrapper Maintenance**: No custom wrappers breaking on Angular Aria/Material updates
* **Official Dependencies Only**: No community library risk (ERP 10+ year lifecycle)
* **Direct API Access**: Full power of Angular Aria/Material without abstraction loss
* **Smaller Bundle**: 120KB vs 150KB with Spartan (30KB savings)
* **Clear Patterns**: 4-layer architecture defines when to create components
* **CVA for Primitives**: Industry-standard variant management without framework lock-in

### Negative
* **No Component Gallery**: Must reference Angular Aria/Material docs directly
* **More Boilerplate**: Each menu/dialog requires directive setup vs pre-built component
* **Learning Curve**: Team must learn Angular Aria/Material APIs (vs Spartan abstraction)

### Mitigation
* Create **pattern examples** in `shared/components/README.md` (copy-paste templates)
* Document **common usages** in migration guide (menu, dialog, select patterns)
* **CVA directives** reduce boilerplate for primitives (button, badge, card)
* For ERP systems, learning official APIs is more valuable than learning community wrappers

## Why Not Spartan?
Spartan wraps the exact same libraries (Angular Aria/CDK) we'd use directly:
* Adds abstraction layer without reducing API complexity
* Still requires learning Angular Aria underneath (for debugging, advanced features)
* Generated components (Helm CLI) create indirection without functional value
* Bundle cost: 30KB+ for wrapper code that doesn't simplify the underlying APIs
* Community maintenance risk: Not official Angular, dependent on maintainer availability

## Why Not Custom Wrappers?
* **False Portability**: Switching from Aria to another library requires rewriting ALL usages (different APIs) regardless of wrapper existence
* **Maintenance Burden**: Every Angular Aria update requires testing and updating wrappers
* **Pass-Through Hell**: Wrappers that just proxy inputs/outputs add zero business value
* **Abstraction Loss**: Wrappers hide powerful features of underlying libraries

## When to Create Components
Only create custom components when **ALL** of these conditions are met:
1. **10+ Usages**: Component is used in at least 10 places
2. **Business Logic**: Adds domain-specific behavior (not just styling)
3. **Reduces Duplication**: Eliminates repeated business patterns
4. **Stable API**: Underlying library API is stable (not changing every version)

## Supersedes
* ADR 0011 (Entity Management UI Patterns)
