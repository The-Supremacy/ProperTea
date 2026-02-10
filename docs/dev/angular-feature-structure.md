# Angular Feature Structure

Reference for how features are organized in the Landlord Portal.

## Directory Layout

```
src/app/features/{feature-name}/
├── routes.ts                               # Lazy-loaded route definitions
├── models/
│   └── {feature-name}.models.ts            # TypeScript interfaces
├── services/
│   └── {feature-name}.service.ts           # HTTP + state service
├── validators/                             # Optional: async validators
│   └── {feature-name}.validators.ts
├── list-view/
│   ├── {feature-name}-list.component.ts
│   └── {feature-name}-list.component.html  # External only if large
├── details/
│   ├── {feature-name}-details.component.ts
│   └── {feature-name}-details.component.html
└── create-drawer/
    └── create-{feature-name}-drawer.component.ts
```

## Key Conventions

**Components**: Always `OnPush`. Use `input()` / `output()` functions, `inject()` for DI. Signals for state, `computed()` for derived values. Prefer inline templates for small components.

**Routes**: Lazy-loaded via `loadChildren` in `app.routes.ts`. Each feature exports `{featureName}Routes`.

**Services**: `providedIn: 'root'` for singletons. Inject `HttpClient`. Expose signals for shared state.

**List views**: Use `EntityListViewComponent` with TanStack Table column definitions. Angular Aria `ngMenu` for row actions.

**Forms**: Reactive Forms only. Field-level validation with inline error messages. Async validators for uniqueness checks.

**i18n**: All user-visible strings via Transloco. Keys in `assets/i18n/{locale}.json`. Two-tier: `common.*` + `{feature}.*`.

## Shared Components

Located in `src/shared/components/`:

| Component | Type | Pattern |
|---|---|---|
| `[appBtn]` | CVA Directive | Variant-based button styling |
| `[appBadge]` | CVA Directive | Status badges |
| `app-drawer` | Component | Side panel for creation flows |
| `app-entity-list-view` | Component | Generic TanStack Table wrapper |
| `app-form-field` | Component | Label + input + error layout |
| `app-select` | Component | Angular Aria Combobox + Listbox |
| `app-confirm-dialog` | Component | Material Dialog for destructive actions |
| `app-spinner` | Component | Loading indicator |

## Reference Implementation

See `src/app/features/companies/` for a complete example of all patterns.
