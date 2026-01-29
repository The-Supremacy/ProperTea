# Landlord Portal Frontend

Angular application for the ProperTea landlord portal.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Folder Structure Template

```
src/app/
├── app.ts
├── app.config.ts
├── app.routes.ts
├── auth/                        # Authentication feature
│   ├── README.md
│   ├── guards/
│   │   ├── authenticated.guard.ts
│   │   └── unauthenticated-only.guard.ts
│   ├── pages/
│   │   └── login/
│   └── services/
│       └── auth.service.ts
├── core/                        # Shared core module
│   ├── README.md
│   ├── guards/                  # Shared guards
│   ├── interceptors/            # HTTP interceptors
│   │   ├── index.ts
│   │   ├── correlation-id.interceptor.ts
│   │   └── error.interceptor.ts
│   ├── models/                  # Shared models
│   │   └── error.models.ts
│   └── services/                # Core services
│       ├── config.service.ts
│       └── error-translator.service.ts
├── features/                    # Feature modules
│   └── {feature-name}/
│       ├── README.md            # Feature documentation
│       ├── components/          # Dumb components (reusable UI)
│       │   └── {component}/
│       ├── config/              # Feature constants
│       │   └── {feature}.constants.ts
│       ├── models/              # TypeScript interfaces
│       │   └── {feature}.models.ts
│       ├── pages/               # Smart components (route-level)
│       │   └── {page}/
│       ├── services/            # HTTP clients
│       │   └── {feature}.service.ts
│       ├── state/               # State management (when needed)
│       │   └── {feature}.store.ts
│       ├── validators/          # Custom form validators
│       │   └── {feature}.validators.ts
│       └── {feature}.routes.ts
├── i18n/                        # Internationalization
│   ├── components/
│   └── {lang}.json
├── layout/                      # App layout components
│   ├── header/
│   ├── footer/
│   └── sidebar/
└── shared/                      # Shared components
    └── components/
```

## Design Principles

### Pages vs Components
- **Pages**: Smart components with routing, state, API calls
- **Components**: Dumb reusable UI pieces with @Input/@Output
- Pages always use components, never the other way around

### Feature Organization
- Each feature is self-contained
- Feature owns its models, services, validators, constants
- Feature-specific routing in `{feature}.routes.ts`

### State Management
- Use Signals for local component state
- Use Services with Signals for shared state
- Add `state/` folder when complexity requires formal state management

### Forms
- Use Reactive Forms only
- Extract validators to `validators/` folder
- Keep form logic in page components

### Styling
- Tailwind CSS utility classes
- Avoid custom CSS unless necessary
- Component-specific styles in `.scss` files

### Internationalization
- Use Transloco for all user-facing text
- No hardcoded strings
- Translations in `i18n/{lang}.json`

### HTTP
- Services use HttpClient
- All requests pass through interceptors (correlation ID, error handling)
- Cookie-based authentication (no Bearer tokens)

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
