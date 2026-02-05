# Component Examples & Patterns

This guide demonstrates real-world usage of the Pure Angular Component Strategy.

## Quick Reference

| Need | Solution | Example |
|------|----------|---------|
| **Styled button** | CVA directive | `<button appBtn variant="destructive">` |
| **Status badge** | CVA directive | `<span appBadge variant="outline">` |
| **Dropdown menu** | Angular Aria direct | `<button [ngMenuTrigger]="menu">` |
| **Date picker** | Material direct | `<mat-datepicker #picker />` |
| **Property list** | Business component | `<app-property-table [data]="props">` |

## Pattern 1: CVA Directives (Primitives)

### Button Variants
```html
<!-- Primary action -->
<button appBtn>Save Changes</button>

<!-- Destructive action -->
<button appBtn variant="destructive">Delete Property</button>

<!-- Secondary action -->
<button appBtn variant="outline">Cancel</button>

<!-- Tertiary action -->
<button appBtn variant="ghost">Learn More</button>

<!-- Link style -->
<button appBtn variant="link">View Details</button>

<!-- Icon button -->
<button appBtn variant="ghost" size="icon">
  <ng-icon name="lucideMoreVertical" />
</button>

<!-- Small button -->
<button appBtn size="sm">Add</button>

<!-- Large button -->
<button appBtn size="lg">Get Started</button>
```

### Badge Variants
```html
<!-- Status indicators -->
<span appBadge>Active</span>
<span appBadge variant="secondary">Draft</span>
<span appBadge variant="destructive">Overdue</span>
<span appBadge variant="outline">Pending</span>

<!-- In context -->
<div class="flex items-center gap-2">
  <h3>Property ABC-123</h3>
  <span appBadge variant="secondary">Draft</span>
</div>
```

## Pattern 2: Angular Aria (Direct Usage)

### Menu Pattern
```typescript
// component.ts
@Component({
  template: `
    <button appBtn variant="outline" [ngMenuTrigger]="actions">
      <ng-icon name="lucideMoreVertical" />
      Actions
    </button>

    <ng-template #actions>
      <div ngMenu class="min-w-[200px] rounded-md border bg-popover p-1 shadow-md">
        <button ngMenuItem (click)="edit()" class="menu-item">
          <ng-icon name="lucideEdit" />
          Edit Property
        </button>
        <button ngMenuItem (click)="duplicate()" class="menu-item">
          <ng-icon name="lucideCopy" />
          Duplicate
        </button>
        <div role="separator" class="my-1 h-px bg-border"></div>
        <button ngMenuItem (click)="delete()" class="menu-item text-destructive">
          <ng-icon name="lucideTrash" />
          Delete
        </button>
      </div>
    </ng-template>
  `,
  styles: [`
    .menu-item {
      @apply flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm;
      @apply cursor-pointer outline-none hover:bg-accent focus:bg-accent;
    }
  `]
})
```

### User Menu (Real Example from Header)
```typescript
@Component({
  template: `
    <button
      appBtn
      variant="ghost"
      size="icon"
      ngMenuTrigger
      [menu]="userMenu()"
      class="rounded-full">
      <ng-icon name="lucideUser" />
    </button>

    <ng-template [cdkConnectedOverlay]="{origin, usePopover: 'inline'}">
      <div ngMenu #userMenu="ngMenu" class="min-w-56 rounded-md border bg-popover p-1">
        <ng-template ngMenuContent>
          <!-- Section: My Account -->
          <div class="px-2 py-1.5 text-sm font-semibold">My Account</div>
          <div role="separator" class="my-1 h-px bg-border"></div>

          <div ngMenuItem (click)="profile()" class="menu-item">
            <ng-icon name="lucideUser" />
            Profile
          </div>
          <div ngMenuItem (click)="preferences()" class="menu-item">
            <ng-icon name="lucideSettings" />
            Preferences
          </div>

          <div role="separator" class="my-1 h-px bg-border"></div>

          <!-- Section: Theme -->
          <div ngMenuItem (click)="toggleTheme()" class="menu-item">
            <ng-icon [name]="theme() === 'dark' ? 'lucideSun' : 'lucideMoon'" />
            {{ theme() === 'dark' ? 'Light Mode' : 'Dark Mode' }}
          </div>

          <div role="separator" class="my-1 h-px bg-border"></div>

          <div ngMenuItem (click)="signOut()" class="menu-item">
            <ng-icon name="lucideLogOut" />
            Sign Out
          </div>
        </ng-template>
      </div>
    </ng-template>
  `
})
```

## Pattern 3: Material (Direct Usage)

### Date Picker
```typescript
@Component({
  imports: [
    ReactiveFormsModule,
    MatDatepickerModule,
    MatInputModule,
    MatFormFieldModule
  ],
  template: `
    <mat-form-field class="w-full">
      <mat-label>Start Date</mat-label>
      <input
        matInput
        [matDatepicker]="startPicker"
        [formControl]="startDate"
        placeholder="Select start date">
      <mat-datepicker-toggle matSuffix [for]="startPicker" />
      <mat-datepicker #startPicker />
      @if (startDate.errors?.['required']) {
        <mat-error>Start date is required</mat-error>
      }
    </mat-form-field>
  `
})
export class DateRangeFormComponent {
  startDate = new FormControl<Date | null>(null, Validators.required);
}
```

### Date Range Picker
```html
<div class="flex gap-4">
  <mat-form-field class="flex-1">
    <mat-label>Start Date</mat-label>
    <input matInput [matDatepicker]="startPicker" [formControl]="startDate">
    <mat-datepicker-toggle matSuffix [for]="startPicker" />
    <mat-datepicker #startPicker />
  </mat-form-field>

  <mat-form-field class="flex-1">
    <mat-label>End Date</mat-label>
    <input matInput [matDatepicker]="endPicker" [formControl]="endDate">
    <mat-datepicker-toggle matSuffix [for]="endPicker" />
    <mat-datepicker #endPicker />
  </mat-form-field>
</div>
```

## Pattern 4: Business Components

### When to Create
Only create custom components when **ALL** conditions are met:
1. ✅ Used in 10+ places
2. ✅ Adds business logic (not just styling)
3. ✅ Reduces duplication of domain patterns
4. ✅ Stable API (underlying library not changing)

### Property Table (Example)
```typescript
// shared/components/property-table/property-table.component.ts
@Component({
  selector: 'app-property-table',
  template: `
    <div class="property-table">
      <div class="flex items-center justify-between mb-4">
        <input
          type="search"
          placeholder="Search properties..."
          [value]="searchQuery()"
          (input)="search($event)"
          class="input w-80">

        <button appBtn (click)="addProperty.emit()">
          <ng-icon name="lucidePlus" />
          Add Property
        </button>
      </div>

      <tanstack-table
        [data]="filteredData()"
        [columns]="propertyColumns"
        (rowClick)="propertyClick.emit($event)">
        <!-- Business logic: Property-specific rendering, validation, actions -->
      </tanstack-table>
    </div>
  `
})
export class PropertyTableComponent {
  data = input.required<Property[]>();

  searchQuery = signal('');

  // Output: Business events
  propertyClick = output<Property>();
  addProperty = output<void>();

  // Business logic: Property-specific columns
  propertyColumns = [
    {
      id: 'name',
      header: 'Property Name',
      cell: (property: Property) => this.renderPropertyName(property),
    },
    {
      id: 'units',
      header: 'Units',
      cell: (property: Property) => {
        const count = property.units.length;
        return `${count} unit${count !== 1 ? 's' : ''}`;
      },
    },
    {
      id: 'status',
      header: 'Status',
      cell: (property: Property) => this.renderStatus(property),
    },
  ];

  // Business logic: Filtered data
  filteredData = computed(() => {
    const query = this.searchQuery().toLowerCase();
    if (!query) return this.data();
    return this.data().filter(p =>
      p.name.toLowerCase().includes(query) ||
      p.address.toLowerCase().includes(query)
    );
  });

  search(event: Event) {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  private renderPropertyName(property: Property) {
    // Business logic: Custom rendering with navigation
  }

  private renderStatus(property: Property) {
    // Business logic: Status badge with domain rules
    const variant = property.isActive ? 'default' : 'secondary';
    return `<span appBadge variant="${variant}">${property.status}</span>`;
  }
}

// Usage in features:
// <app-property-table
//   [data]="properties()"
//   (propertyClick)="navigateToProperty($event)"
//   (addProperty)="openAddDialog()" />
```

## Real-World Form Example

```typescript
@Component({
  template: `
    <form [formGroup]="form" (ngSubmit)="save()" class="space-y-6">
      <!-- Text Input -->
      <div>
        <label for="name" class="label">Property Name</label>
        <input
          id="name"
          type="text"
          formControlName="name"
          class="input w-full"
          placeholder="Enter property name">
        @if (form.controls.name.errors?.['required']) {
          <p class="mt-1 text-sm text-destructive">Name is required</p>
        }
      </div>

      <!-- Date Range -->
      <div class="grid grid-cols-2 gap-4">
        <mat-form-field>
          <mat-label>Lease Start</mat-label>
          <input matInput [matDatepicker]="startPicker" formControlName="leaseStart">
          <mat-datepicker-toggle matSuffix [for]="startPicker" />
          <mat-datepicker #startPicker />
        </mat-form-field>

        <mat-form-field>
          <mat-label>Lease End</mat-label>
          <input matInput [matDatepicker]="endPicker" formControlName="leaseEnd">
          <mat-datepicker-toggle matSuffix [for]="endPicker" />
          <mat-datepicker #endPicker />
        </mat-form-field>
      </div>

      <!-- Status Badge Display -->
      <div>
        <label class="label">Status</label>
        <div class="flex gap-2">
          <span appBadge [variant]="statusVariant()">
            {{ form.value.status }}
          </span>
        </div>
      </div>

      <!-- Actions -->
      <div class="flex gap-2 justify-end">
        <button type="button" appBtn variant="outline" (click)="cancel()">
          Cancel
        </button>
        <button type="submit" appBtn [disabled]="form.invalid">
          Save Property
        </button>
      </div>
    </form>
  `
})
export class PropertyFormComponent {
  form = new FormGroup({
    name: new FormControl('', Validators.required),
    leaseStart: new FormControl<Date | null>(null),
    leaseEnd: new FormControl<Date | null>(null),
    status: new FormControl('active'),
  });

  statusVariant = computed(() => {
    const status = this.form.value.status;
    return status === 'active' ? 'default' : 'secondary';
  });

  save() {
    if (this.form.valid) {
      // Save logic
    }
  }

  cancel() {
    // Cancel logic
  }
}
```

## Common Patterns

### Loading State Button
```html
<button appBtn [disabled]="loading()">
  @if (loading()) {
    <ng-icon name="lucideLoader2" class="animate-spin" />
  }
  {{ loading() ? 'Saving...' : 'Save' }}
</button>
```

### Confirmation Dialog (Angular Aria)
```typescript
@Component({
  template: `
    <button appBtn variant="destructive" [ngDialogTrigger]="confirm">
      Delete Property
    </button>

    <ng-template #confirm>
      <div ngDialog class="max-w-md rounded-lg border bg-background p-6 shadow-lg">
        <h2 class="text-lg font-semibold">Confirm Deletion</h2>
        <p class="mt-2 text-sm text-muted-foreground">
          Are you sure you want to delete this property? This action cannot be undone.
        </p>
        <div class="mt-6 flex gap-2 justify-end">
          <button appBtn variant="outline" ngDialogClose>Cancel</button>
          <button appBtn variant="destructive" (click)="confirmDelete()">Delete</button>
        </div>
      </div>
    </ng-template>
  `
})
```

### Action Menu with Status
```html
<div class="flex items-center gap-2">
  <span appBadge [variant]="property.status === 'active' ? 'default' : 'secondary'">
    {{ property.status }}
  </span>

  <button appBtn variant="ghost" size="icon" [ngMenuTrigger]="actions">
    <ng-icon name="lucideMoreVertical" />
  </button>

  <ng-template #actions>
    <div ngMenu class="min-w-[200px] rounded-md border bg-popover p-1">
      <button ngMenuItem (click)="edit()" class="menu-item">Edit</button>
      <button ngMenuItem (click)="duplicate()" class="menu-item">Duplicate</button>
      @if (property.status === 'draft') {
        <button ngMenuItem (click)="publish()" class="menu-item">Publish</button>
      }
      <div role="separator" class="my-1 h-px bg-border"></div>
      <button ngMenuItem (click)="delete()" class="menu-item text-destructive">
        Delete
      </button>
    </div>
  </ng-template>
</div>
```

## Anti-Pattern Examples

### ❌ DON'T: Create Wrapper Components

```typescript
// BAD: This adds ZERO value
@Component({
  selector: 'app-menu',
  template: `<div ngMenu>...</div>`
})
export class MenuComponent {
  items = input<MenuItem[]>();  // Just proxying to ngMenu
}

// GOOD: Use Angular Aria directly
<button [ngMenuTrigger]="menu">Actions</button>
<ng-template #menu>
  <div ngMenu>
    @for (item of menuItems; track item.id) {
      <button ngMenuItem (click)="item.action()">{{ item.label }}</button>
    }
  </div>
</ng-template>
```

### ❌ DON'T: Wrap Material Components

```typescript
// BAD: Pass-through hell
@Component({
  selector: 'app-datepicker',
  template: `<mat-datepicker [startDate]="startDate" [minDate]="minDate" ... />`
})
export class DatepickerWrapperComponent {
  startDate = input<Date>();    // Proxying
  minDate = input<Date>();      // Proxying
  // ... 20+ more proxied inputs
}

// GOOD: Use Material directly
<mat-form-field>
  <input matInput [matDatepicker]="picker" [formControl]="date">
  <mat-datepicker #picker />
</mat-form-field>
```

## Summary

| Layer | Use | Don't Wrap | Example |
|-------|-----|------------|---------|
| **Primitives** | CVA directives | Native HTML | `<button appBtn>` |
| **Interactive** | Angular Aria direct | ngMenu, ngDialog | `<button [ngMenuTrigger]>` |
| **Complex** | Material direct | mat-datepicker | `<mat-datepicker #picker>` |
| **Business** | Custom only if needed | Add logic, not proxy | `<app-property-table>` |

**Key Principle**: Only wrap when adding business value, not for abstraction's sake.
