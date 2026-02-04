# Landlord Portal Frontend

Angular application for the ProperTea landlord portal.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Folder Structure

```
src/
├── app/
│   ├── app.component.ts
│   ├── app.config.ts
│   ├── app.routes.ts
│   ├── core/                        # Singleton services, guards, interceptors
│   │   ├── README.md
│   │   ├── guards/
│   │   │   ├── auth.guard.ts
│   │   │   └── unsaved-changes.guard.ts
│   │   ├── interceptors/
│   │   │   ├── correlation-id.interceptor.ts
│   │   │   ├── error.interceptor.ts
│   │   │   └── tenant-context.interceptor.ts
│   │   ├── models/
│   │   │   ├── error.models.ts
│   │   │   ├── pagination.models.ts
│   │   │   └── session.models.ts
│   │   └── services/
│   │       ├── config.service.ts
│   │       ├── error-handler.service.ts
│   │       ├── session.service.ts
│   │       └── theme.service.ts
│   ├── features/                    # Feature modules (vertical slices)
│   │   ├── auth/
│   │   │   ├── guards/
│   │   │   │   └── authenticated.guard.ts
│   │   │   ├── pages/
│   │   │   │   ├── login/
│   │   │   │   └── callback/
│   │   │   ├── services/
│   │   │   │   └── auth.service.ts
│   │   │   └── auth.routes.ts
│   │   ├── companies/               # Example entity feature
│   │   │   ├── components/          # Feature-specific components
│   │   │   │   ├── company-form/
│   │   │   │   ├── company-filters/
│   │   │   │   └── company-actions-menu/
│   │   │   ├── models/
│   │   │   │   └── company.models.ts
│   │   │   ├── pages/
│   │   │   │   ├── company-list/
│   │   │   │   └── company-detail/
│   │   │   ├── services/
│   │   │   │   └── company.service.ts
│   │   │   ├── validators/
│   │   │   │   └── company.validators.ts
│   │   │   ├── companies.constants.ts
│   │   │   └── companies.routes.ts
│   │   └── dashboard/
│   │       ├── components/
│   │       ├── pages/
│   │       │   └── dashboard-home/
│   │       └── dashboard.routes.ts
│   ├── layout/                      # Shell layout components
│   │   ├── app-shell/
│   │   ├── header/
│   │   ├── sidebar/
│   │   └── footer/
│   └── shared/                      # Shared UI components & utilities
│       ├── components/              # Reusable dumb components
│       │   ├── data-table/
│       │   ├── form-field-error/
│       │   ├── loading-spinner/
│       │   └── page-header/
│       ├── directives/
│       │   └── auto-focus.directive.ts
│       ├── pipes/
│       │   ├── localized-date.pipe.ts
│       │   └── safe-html.pipe.ts
│       └── utils/
│           ├── form.utils.ts
│           └── date.utils.ts
├── assets/
│   ├── i18n/                        # Translation files
│   │   ├── en.json
│   │   └── uk.json
│   └── images/
├── styles/
│   ├── material/                    # Material theme customization
│   │   ├── _theme.scss
│   │   ├── _typography.scss
│   │   └── _component-overrides.scss
│   └── utilities/                   # Custom utility classes
│       └── _spacing.scss
└── styles.css                       # Global styles with Tailwind import
```

## Design Principles

### Core vs Shared vs Features
- **Core**: App-wide singleton services, guards, interceptors (imported once in app.config)
- **Shared**: Reusable UI components, directives, pipes, utilities (imported where needed)
- **Features**: Self-contained vertical slices with their own pages, components, services

### Pages vs Components
- **Pages**: Smart components with routing, state, API calls (in `features/{feature}/pages/`)
- **Components**: Dumb reusable UI pieces with `input()`/`output()` signals
- Feature-specific components live in `features/{feature}/components/`
- Generic reusable components live in `shared/components/`

### Feature Organization (Vertical Slice)
- Each feature is self-contained in `features/{feature}/`
- Feature owns its models, services, validators, constants
- Feature-specific routing in `{feature}.routes.ts`
- Components folder contains feature-specific dumb components
- Pages folder contains route-level smart components

### State Management
- Use `signal()` for local component state
- Use `computed()` for derived state
- Services with Signals for shared state across components
- No external state management library needed

### Forms
- Use Reactive Forms only
- Extract custom validators to `validators/` folder within feature
- Keep form logic in page components
- Use Angular Material form fields for consistent styling

### Styling
- Tailwind CSS utility classes for spacing, layout, colors
- Angular Material for UI components (buttons, inputs, dialogs)
- Custom Material theme in `styles/material/`
- Component-specific styles in `.css` files (unless SCSS features needed)
- Use Material's theming system for dark mode support

### Internationalization
- Use Transloco for all user-facing text
- No hardcoded strings in templates or TypeScript
- Translation files in `assets/i18n/{lang}.json`
- Use two-tier structure: `common.*` (shared) + `{feature}.*` (specific)

### HTTP & Authentication
- Services use `HttpClient` with `inject()`
- All requests pass through interceptors (correlation ID, tenant context, error handling)
- Cookie-based authentication (no Bearer tokens in frontend)
- Session context retrieved from BFF `/session` endpoint

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
