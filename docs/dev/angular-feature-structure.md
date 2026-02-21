# Angular Feature Structure

Reference for how features are organized in the Landlord Portal.

## Directory Layout

```
src/app/features/{feature-name}/
├── index.ts                                # Public barrel exports
├── routes.ts                               # Lazy-loaded route definitions
├── models/
│   └── {feature-name}.models.ts            # TypeScript interfaces
├── services/
│   └── {feature-name}.service.ts           # HTTP + state service
├── validators/                             # Optional: async validators
│   └── {feature-name}.validators.ts
├── list-view/
│   └── {feature-name}-list.component.ts    # List page with EntityListViewComponent
├── details/
│   └── {feature-name}-details.component.ts # Detail page with tabs (details + history)
├── audit-log/                              # Optional: audit log tab
│   └── {feature-name}-audit-log.component.ts
├── create-drawer/                          # Optional: create via side drawer
│   └── create-{feature-name}-drawer.component.ts
└── embedded-list/                          # Optional: sub-entity list on parent detail page
    └── {child}-embedded-list.component.ts
```

## Key Conventions

**Components**: Always `OnPush`. Use `input()` / `output()` functions, `inject()` for DI. Signals for state, `computed()` for derived values. Prefer inline templates for small components.

**Routes**: Lazy-loaded via `loadChildren` in `app.routes.ts`. Each feature exports `{featureName}Routes`.

**Services**: `providedIn: 'root'` for singletons. Inject `HttpClient`. Expose signals for shared state.

**List views**: Use `EntityListViewComponent` with TanStack Table column definitions. Angular Aria `ngMenu` for row actions.

**Forms**: Reactive Forms only. Field-level validation with inline error messages. Async validators for uniqueness checks.

**i18n**: All user-visible strings via Transloco. Keys in `assets/i18n/{locale}.json`. Two-tier: `common.*` + `{feature}.*`.

## Shared Components

Located in `src/shared/`:

### `src/shared/components/`

| Component | Pattern |
|---|---|
| `app-entity-list-view` | Generic TanStack Table wrapper with filters, pagination, empty states, row actions |
| `app-entity-details-view` | Layout for detail pages with tabs (details + history) |
| `app-confirm-dialog` | Spartan Alert Dialog for destructive actions |
| `app-address-form` | Reusable reactive address sub-form (street, city, zip, country) |
| `app-async-select` | Async select with search (e.g., company/property pickers) |
| `app-autocomplete` | Autocomplete input with search suggestions |
| `app-timeline` | Audit log timeline renderer |
| `app-icon` | Lucide icon wrapper |
| `app-logo` | Application logo |

### `src/shared/components/ui/` (Spartan/Helm primitives)

Headless UI primitives (accordion, alert-dialog, autocomplete, badge, breadcrumb, button, card, checkbox, dialog, dropdown-menu, empty, form-field, icon, input, input-group, label, navigation-menu, pagination, popover, select, separator, sheet, skeleton, sonner, spinner, switch, table, tabs, textarea, tooltip).

### `src/shared/directives/`

| Directive | Pattern |
|---|---|
| `StatusBadgeDirective` | Maps status strings to colored badges |
| `UppercaseInputDirective` | Forces input to uppercase (for code fields) |

## Reference Implementation

See `src/app/features/companies/` for a simple single-entity example, or `src/app/features/properties/` and `src/app/features/buildings/` for features with embedded lists, audit logs, and detail views with multiple tabs.
