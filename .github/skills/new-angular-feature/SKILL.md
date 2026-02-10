---
name: new-angular-feature
description: Scaffold a new Angular feature in the ProperTea Landlord Portal. Use when creating a new page, CRUD interface, or feature module in the frontend.
---

# Scaffold a New Angular Feature

You are scaffolding a new feature for the ProperTea Landlord Portal.

## Before You Start
1. Check existing features in `src/app/features/` for pattern reference.
2. Confirm the BFF endpoint exists or plan it alongside this feature.

## File Structure to Generate

```
src/app/features/{feature-name}/
├── routes.ts                     # Lazy-loaded route definitions
├── models/
│   └── {feature-name}.models.ts  # TypeScript interfaces
├── services/
│   └── {feature-name}.service.ts # HTTP service
├── list-view/
│   ├── {feature-name}-list.component.ts
│   └── {feature-name}-list.component.html   # Only if template is large
├── details/
│   ├── {feature-name}-details.component.ts
│   └── {feature-name}-details.component.html
└── create-drawer/                # If feature supports creation
    └── create-{feature-name}-drawer.component.ts
```

## Routes File

```typescript
import { Routes } from '@angular/router';

export const {featureName}Routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./list-view/{feature-name}-list.component')
        .then(m => m.{FeatureName}ListComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./details/{feature-name}-details.component')
        .then(m => m.{FeatureName}DetailsComponent),
  },
];
```

Register in `app.routes.ts`:
```typescript
{
  path: '{feature-name}',
  loadChildren: () =>
    import('./features/{feature-name}/routes')
      .then(m => m.{featureName}Routes),
},
```

## Service Pattern

```typescript
@Injectable({ providedIn: 'root' })
export class {FeatureName}Service {
  private http = inject(HttpClient);

  list(params?: PaginationParams) {
    return this.http.get<PagedResult<{FeatureName}>>('/api/{feature-name}', { params });
  }

  getById(id: string) {
    return this.http.get<{FeatureName}>(`/api/{feature-name}/${id}`);
  }

  create(request: Create{FeatureName}Request) {
    return this.http.post<{ id: string }>('/api/{feature-name}', request);
  }
}
```

## Component Pattern

```typescript
@Component({
  selector: 'app-{feature-name}-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [/* only what's needed */],
  template: `
    <!-- Use TanStack Table via EntityListViewComponent for list views -->
    <!-- Use native control flow: @if, @for -->
    <!-- All strings via transloco pipe -->
  `,
})
export class {FeatureName}ListComponent {
  private service = inject({FeatureName}Service);

  items = signal<{FeatureName}[]>([]);
  loading = signal(false);

  totalCount = computed(() => this.items().length);
}
```

## Component Rules
- `ChangeDetectionStrategy.OnPush` on every component.
- `input()` / `output()` functions, not decorators.
- `inject()` for DI, not constructor.
- Signals + `computed()` for all state. No `BehaviorSubject`.
- Native control flow (`@if`, `@for`). Never `*ngIf`, `*ngFor`.
- Never `ngClass`/`ngStyle`. Use `class`/`style` bindings.
- All text through Transloco (`| transloco`).
- Use `EntityListViewComponent` + TanStack Table for data tables.
- Use Angular Aria for interactive widgets. Angular Material for overlays.

## Checklist
- [ ] Routes defined and lazy-loaded
- [ ] Routes registered in `app.routes.ts`
- [ ] Models defined as TypeScript interfaces
- [ ] Service created with `inject(HttpClient)`
- [ ] Components use `OnPush`, signals, `input()`/`output()`
- [ ] All user-visible strings use Transloco
- [ ] Translation keys added to `assets/i18n/en.json` and `assets/i18n/uk.json`
- [ ] BFF endpoint exists or is being created alongside
- [ ] Accessibility: ARIA attributes, focus management, contrast
