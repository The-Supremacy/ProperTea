# Drawer Pattern

Drawers are slide-in panels used for filters, column selection, and forms throughout the application.

## Pattern Overview

All drawers follow a consistent structure defined in `entity-list-view` and reusable for custom drawers.

### Structure

```html
<!-- Backdrop -->
<div class="fixed inset-0 z-50 bg-background/80 backdrop-blur-[2px]" (click)="close()"></div>

<!-- Drawer Content -->
<div class="fixed inset-y-0 right-0 z-50 w-full sm:max-w-md border-l bg-background shadow-lg">
  <!-- Header -->
  <div class="flex items-center justify-between border-b p-4">
    <h2 class="text-lg font-semibold">{{ title }}</h2>
    <button appBtn variant="ghost" size="icon" (click)="close()">
      <app-icon name="close" [size]="20" />
    </button>
  </div>

  <!-- Body -->
  <div class="flex-1 overflow-y-auto p-4">
    <!-- Content here -->
  </div>

  <!-- Footer -->
  <div class="flex gap-2 border-t p-4">
    <button appBtn variant="outline" class="flex-1">{{ secondaryAction }}</button>
    <button appBtn class="flex-1">{{ primaryAction }}</button>
  </div>
</div>
```

## Style Guidelines

### Spacing
- **Header/Body/Footer padding**: `p-4` (16px)
- **Button gap**: `gap-2` (8px)

### Footer Button Patterns

#### Two Equal-Width Buttons (Filter/Column Selection Pattern)
Used when both actions are equally important:
```html
<div class="flex gap-2 border-t p-4">
  <button appBtn variant="outline" class="flex-1">Clear</button>
  <button appBtn class="flex-1">Apply</button>
</div>
```

#### Two Actions, Right-Aligned (Form Pattern)
Used for forms with Cancel (secondary) and Submit (primary):
```html
<div class="flex gap-2 border-t p-4">
  <button appBtn variant="outline" class="flex-1">Cancel</button>
  <button appBtn class="flex-1">Submit</button>
</div>
```

**Note**: Previously, form drawers used `justify-end` without `flex-1`, but we've standardized on equal-width buttons for consistency across all drawers.

### Width Guidelines
- **Mobile**: `w-full` (full screen)
- **Desktop**: `sm:max-w-md` (448px) for most drawers
- **Wide**: `sm:max-w-lg` (512px) or `sm:max-w-xl` (576px) for complex forms

## Examples

### Filter Drawer (entity-list-view)
```html
<div class="flex gap-2 border-t p-4">
  <button appBtn variant="outline" class="flex-1" (click)="clearFilters()">
    {{ 'common.clearFilters' | transloco }}
  </button>
  <button appBtn class="flex-1" (click)="applyFilters()">
    {{ 'common.applyFilters' | transloco }}
  </button>
</div>
```

### Column Selection Drawer (entity-list-view)
```html
<div class="flex gap-2 border-t p-4">
  <button appBtn variant="outline" class="flex-1" (click)="resetColumnVisibility()">
    {{ 'common.reset' | transloco }}
  </button>
  <button appBtn class="flex-1" (click)="toggleColumnDrawer()">
    {{ 'common.done' | transloco }}
  </button>
</div>
```

### Create Company Drawer
```html
<div class="flex gap-2 border-t p-4">
  <button appBtn variant="outline" class="flex-1" [disabled]="isSubmitting()" (click)="close()">
    {{ 'common.cancel' | transloco }}
  </button>
  <button appBtn class="flex-1" [disabled]="!canSubmit()">
    @if (isSubmitting()) {
      <app-spinner size="sm" />
    }
    {{ 'common.create' | transloco }}
  </button>
</div>
```

## Transitions

Use Tailwind transition utilities:
```html
<!-- Backdrop fade -->
<div class="transition-opacity duration-300" [class.opacity-0]="!open" [class.opacity-100]="open">

<!-- Drawer slide -->
<div class="transition-transform duration-300" [class.translate-x-full]="!open" [class.translate-x-0]="open">
```

## Accessibility

- Header must have `<h2>` with semantic level
- Close button must have `aria-label` for screen readers
- Backdrop click should close drawer
- ESC key should close drawer (implement in component `@HostListener`)
- Focus management: trap focus inside drawer when open

## When to Use

- ✅ **Filters**: Quick filter application without leaving context
- ✅ **Column Selection**: Toggling column visibility
- ✅ **Quick Forms**: Create/edit with minimal required fields
- ✅ **Settings**: Contextual settings panels

- ❌ **Complex Multi-Step Forms**: Use full page instead
- ❌ **Confirmations**: Use Dialog component
- ❌ **Critical Warnings**: Use Dialog component
