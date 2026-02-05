# Migration Guide: Pure Headless UI Strategy

**Target Architecture**: Pure Headless (Angular Aria + TanStack + Tailwind)
**Date**: 2026-02-05
**Priority**: High

## 1. Executive Summary
This guide outlines the steps to migrate the frontend from the "Spartan/Material Mix" to the **Pure Headless** architecture defined in ADR 0012.

**Key Changes:**
* **Removal**: All `@spartan-ng/*` packages and `hlm` directives.
* **New Core**: `@angular/aria` for interaction logic, `@tanstack/angular-table` for grids.
* **Styling**: Tailwind v4 (`styles.css`) becomes the Single Source of Truth.
* **Material**: Retained *only* for Datepicker/Slider, "slaved" to Tailwind variables.
* **Location**: All UI components move to `src/app/shared/components`.

---

## 2. Phase 1: The Purge (Cleanup)

Before building, we must remove the "Directive Soup" created by Spartan.

### 2.1 Uninstall Dependencies
Remove all Spartan-related packages to ensure no "community wrapper" code remains.
npm uninstall @spartan-ng/helm-menu @spartan-ng/helm-button @spartan-ng/helm-table @spartan-ng/helm-core

## 2. Phase 1: The Purge (Cleanup)

Before building, we must remove the "Directive Soup" created by Spartan.

### 2.1 Uninstall Dependencies
Remove all Spartan-related packages to ensure no "community wrapper" code remains.

npm uninstall @spartan-ng/helm-menu @spartan-ng/helm-button @spartan-ng/helm-table @spartan-ng/helm-core

2.2 Clean Templates

Search your codebase (Ctrl+Shift+F) and remove these directives from your HTML templates:

    hlmBtn

    hlmMenu, hlmMenuItem

    hlmTable, hlmTh, hlmTd

2.3 Delete Legacy UI Folder

Delete the libs/ui folder (or wherever Spartan generated its components). We will rebuild clean versions in src/app/shared/components.
3. Phase 2: The Foundation (Installation)

Install the "Logic Engines" that will power our headless components.
3.1 Install Core Libraries
Bash

npm install @angular/aria @angular/cdk @tanstack/angular-table class-variance-authority lucide-angular

3.2 Install Material (Exceptions Only)

We need the complex logic for Datepickers, which is too risky to build from scratch.
Bash

npm install @angular/material

3.3 Configure Angular.json

We load a prebuilt Material theme to register the CSS variables (skeleton), which we will later override. Add this before src/styles.css.

File: angular.json
JSON

"styles": [
  "node_modules/@angular/material/prebuilt-themes/azure-blue.css",
  "src/styles.css"
]

4. Phase 3: The "Tailwind Master" Configuration

We configure styles.css to act as the Bridge. This forces Material components to use your Tailwind variables.

File: src/styles.css
CSS

@import "tailwindcss";
@import '@angular/cdk/overlay-prebuilt.css';

/* 1. DEFINE TAILWIND AS MASTER (Source of Truth) */
@theme {
  --color-background: hsl(0 0% 100%);
  --color-foreground: hsl(222.2 84% 4.9%);

  --color-primary: hsl(222.2 47.4% 11.2%);
  --color-primary-foreground: hsl(210 40% 98%);

  --color-muted: hsl(210 40% 96.1%);
  --color-muted-foreground: hsl(215.4 16.3% 46.9%);

  --color-border: hsl(214.3 31.8% 91.4%);
  --color-input: hsl(214.3 31.8% 91.4%);

  --radius-md: 0.5rem;
}

/* 2. THE MATERIAL BRIDGE (Slave Material to Tailwind) */
:root {
  /* Map Material Primary to Tailwind Primary */
  --mat-sys-primary: var(--color-primary);
  --mat-sys-on-primary: var(--color-primary-foreground);

  /* Map Backgrounds */
  --mat-sys-surface: var(--color-background);
  --mat-sys-background: var(--color-background);
  --mat-sys-on-surface: var(--color-foreground);

  /* Map Borders & Shapes */
  --mat-sys-outline-variant: var(--color-border);
  --mat-sys-corner-medium: var(--radius-md);

  /* Datepicker Specific Overrides (Flattening the look) */
  --mat-datepicker-container-elevation-shadow: none;
  --mat-datepicker-container-shape: var(--radius-md);
  --mat-datepicker-calendar-date-selected-state-background-color: var(--color-primary);
  --mat-datepicker-calendar-date-selected-state-text-color: var(--color-primary-foreground);
}

/* 3. LAYERS & UTILITIES */
@layer base {
  .dark {
    /* ... keep existing dark mode vars ... */
  }
}

@layer components {
  /* Keep existing custom classes if needed */
  .btn { ... }
}

5. Phase 4: Component Reconstruction

We rebuild the core UI kit in src/app/shared/components.
5.1 Button (Native HTML + CVA)

Strategy: Attribute selector on native element.

File: src/app/shared/components/button/button.component.ts
TypeScript

import { Component, Input, computed } from '@angular/core';
import { cva, type VariantProps } from 'class-variance-authority';

const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      variant: {
        default: 'bg-primary text-primary-foreground hover:bg-primary/90',
        outline: 'border border-input bg-background hover:bg-accent hover:text-accent-foreground',
        ghost: 'hover:bg-accent hover:text-accent-foreground',
      },
      size: {
        default: 'h-10 px-4 py-2',
        icon: 'h-10 w-10',
      },
    },
    defaultVariants: { variant: 'default', size: 'default' },
  }
);

@Component({
  selector: 'button[app-button], a[app-button]',
  standalone: true,
  template: '<ng-content />',
  host: {
    '[class]': 'computedClass()'
  }
})
export class ButtonComponent {
  @Input() variant: VariantProps<typeof buttonVariants>['variant'] = 'default';
  @Input() size: VariantProps<typeof buttonVariants>['size'] = 'default';

  protected computedClass = computed(() =>
    buttonVariants({ variant: this.variant, size: this.size })
  );
}

5.2 Menu (Angular Aria + Data Driven)

Strategy: ariaMenu directive with data input.

File: src/app/shared/components/menu/menu.component.ts
TypeScript

import { Component, Input } from '@angular/core';
import { AriaMenu, AriaMenuItem, AriaMenuTrigger } from '@angular/aria';
import { NgIcon } from '@ng-icons/core';

export interface MenuItem {
  label: string;
  icon?: string;
  action?: () => void;
  type?: 'item' | 'separator';
}

@Component({
  selector: 'app-menu',
  standalone: true,
  imports: [AriaMenu, AriaMenuItem, AriaMenuTrigger, NgIcon],
  template: `
    <button [ariaMenuTriggerFor]="menu" class="inline-flex items-center justify-center rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 border border-input bg-background shadow-sm hover:bg-accent hover:text-accent-foreground h-9 px-4 py-2">
      {{ label }}
      <ng-icon name="lucideChevronDown" class="ml-2 h-4 w-4" />
    </button>

    <ng-template #menu>
      <div ariaMenu class="min-w-[8rem] overflow-hidden rounded-md border border-border bg-popover p-1 text-popover-foreground shadow-md">
        @for (item of items; track item.label) {
          @if (item.type === 'separator') {
            <div class="my-1 h-px bg-muted"></div>
          } @else {
            <div
              ariaMenuItem
              (click)="item.action?.()"
              class="relative flex cursor-default select-none items-center rounded-sm px-2 py-1.5 text-sm outline-none transition-colors focus:bg-accent focus:text-accent-foreground data-[disabled]:pointer-events-none data-[disabled]:opacity-50"
            >
              @if (item.icon) { <ng-icon [name]="item.icon" class="mr-2 h-4 w-4" /> }
              {{ item.label }}
            </div>
          }
        }
      </div>
    </ng-template>
  `
})
export class MenuComponent {
  @Input() label = 'Menu';
  @Input() items: MenuItem[] = [];
}

5.3 Datepicker (Material Wrapper)

Strategy: Wrapped Material component that inherits styles via the "Bridge" in styles.css.

File: src/app/shared/components/datepicker/datepicker.component.ts
TypeScript

import { Component } from '@angular/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';

@Component({
  selector: 'app-datepicker',
  standalone: true,
  imports: [MatDatepickerModule, MatNativeDateModule],
  template: `
    <div class="flex items-center rounded-md border border-input bg-transparent px-3 py-1 shadow-sm transition-colors focus-within:ring-1 focus-within:ring-ring">
      <input
        [matDatepicker]="picker"
        class="flex h-9 w-full bg-transparent py-1 text-sm shadow-sm transition-colors placeholder:text-muted-foreground focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
        placeholder="Pick a date"
      />
      <mat-datepicker-toggle [for]="picker" class="text-muted-foreground hover:text-foreground">
      </mat-datepicker-toggle>
    </div>
    <mat-datepicker #picker></mat-datepicker>
  `
})
export class DatepickerComponent {}

6. Phase 5: Verification

    Build Check: Run ng build to ensure no missing spartan exports.

    Visual Check:

        Does <button app-button variant="destructive"> show as red?

        Does the Datepicker selection color match the Button's primary color? (Verifies the Bridge).

        Does the Menu support arrow key navigation? (Verifies Aria logic).

    Imports Check: Ensure src/app/shared/components is the only directory importing @angular/aria or @angular/material.
