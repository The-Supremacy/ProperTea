# Angular Project Structure

This document describes the organization of the ProperTea Landlord Portal frontend application.

## Overview

The application follows a feature-based architecture with clear separation of concerns:

```
src/app/
├── core/                  # Singleton services and app-wide functionality
│   ├── services/         # Core services (auth, config, etc.)
│   └── index.ts          # Barrel export
├── shared/               # Shared/reusable components, directives, pipes
│   └── index.ts          # Barrel export
├── layout/               # Layout components (nav, header, footer, sidebar)
│   └── index.ts          # Barrel export
├── features/             # Feature modules organized by domain
│   └── organizations/    # Example feature: organizations
│       ├── models/       # Domain models and DTOs
│       ├── services/     # Feature-specific services
│       ├── pages/        # Smart components (containers)
│       ├── components/   # Dumb/presentational components (if needed)
│       ├── routes.ts     # Feature routing configuration
│       └── index.ts      # Barrel export
├── app.routes.ts         # Main application routes
├── app.config.ts         # Application configuration
├── app.ts                # Root component
└── app.html              # Root template
```

## Folder Structure Details

### `/core`
Contains singleton services that are used throughout the application:
- **AuthService**: Handles authentication state using Angular signals and resources
- **ConfigService**: Environment-specific configuration (URLs, etc.)

These services are provided in root and instantiated once per application.

### `/shared`
Contains reusable UI components, directives, and pipes that are used across multiple features:
- Generic components (loading spinners, error messages, etc.)
- Utility directives
- Common pipes

### `/layout`
Contains layout components that define the application shell:
- Navigation components
- Header/footer
- Sidebar
- Page wrappers

### `/features`
Each feature is a self-contained domain module with its own:

#### Structure per feature:
```
features/organizations/
├── models/                    # Domain models
│   └── organization.model.ts
├── services/                  # Business logic services
│   └── organizations.service.ts
├── pages/                     # Smart components (containers)
│   ├── organizations-list/
│   │   ├── organizations-list.component.ts
│   │   ├── organizations-list.component.html
│   │   └── organizations-list.component.scss
│   └── organization-detail/
│       └── ...
├── components/                # Dumb/presentational components
│   └── organization-card/     # (example - create as needed)
├── routes.ts                  # Feature route configuration
└── index.ts                   # Barrel export
```

## Component Types

### Smart Components (Pages)
- Located in `pages/` folder
- Handle data fetching and state management
- Inject services
- Use signals for reactive state
- Pass data down to dumb components

### Dumb Components
- Located in `components/` folder (when needed)
- Receive data via `@Input()`
- Emit events via `@Output()`
- No service injection (except utility services)
- Focused on presentation

## Routing Pattern

### Lazy Loading
Features are lazy-loaded for better performance:

```typescript
// app.routes.ts
{
  path: 'organizations',
  loadChildren: () => import('./features/organizations/routes')
    .then(m => m.organizationsRoutes)
}

// features/organizations/routes.ts
export const organizationsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/organizations-list/...')
  },
  {
    path: ':id',
    loadComponent: () => import('./pages/organization-detail/...')
  }
];
```

## Path Aliases

TypeScript path aliases are configured in `tsconfig.app.json`:

```typescript
import { AuthService } from '@core';
import { SomeSharedComponent } from '@shared';
import { OrganizationsService } from '@features/organizations';
```

Available aliases:
- `@core` → `src/app/core/`
- `@shared` → `src/app/shared/`
- `@features` → `src/app/features/`
- `@layout` → `src/app/layout/`

## Configuration Management

### K8s Environment Strategy
Since all environments (dev, staging, qa, production) run on Kubernetes with consistent service names:

1. **Local Development**: Uses `localhost` URLs for BFF and ZITADEL
2. **K8s Environments**: Uses internal service names (e.g., `http://zitadel:8080`)
3. **Detection**: Based on `window.location.hostname === 'localhost'`

The `ConfigService` handles this automatically, so no environment-specific files are needed.

### Example:
```typescript
// ConfigService returns different URLs based on environment
get zitadelUrl(): string {
  return window.location.hostname === 'localhost'
    ? 'http://localhost:9080'  // Local dev
    : 'http://zitadel:8080';   // K8s
}
```

## Authentication

### No Interceptor Needed
The BFF (Backend for Frontend) pattern handles authentication:
- Session cookies are automatically sent with every request
- No need for token management in the frontend
- No need for auth interceptor to add headers

### Auth State
Auth state is managed using Angular's modern resource API:

```typescript
// AuthService
readonly userResource: ResourceRef<UserInfo | undefined> = resource({
  request: () => this.refreshTrigger(),
  loader: async () => {
    const response = await fetch('/auth/user', { credentials: 'include' });
    return await response.json();
  }
});

// Computed signals derived from resource
readonly isAuthenticated = computed(() =>
  this.userResource.value()?.isAuthenticated ?? false
);
```

## State Management

### Signals-First Approach
- Use Angular signals for reactive state
- Use resources for async data loading
- Use computed signals for derived state
- Avoid RxJS unless truly needed (e.g., WebSockets)

### Example Service Pattern:
```typescript
@Injectable({ providedIn: 'root' })
export class SomeService {
  // State signal
  private readonly items = signal<Item[]>([]);

  // Loading state
  readonly isLoading = signal(false);

  // Public computed signal
  readonly itemsCount = computed(() => this.items().length);

  // Async method that updates signals
  async loadItems(): Promise<void> {
    this.isLoading.set(true);
    try {
      const data = await firstValueFrom(this.http.get<Item[]>('/api/items'));
      this.items.set(data);
    } finally {
      this.isLoading.set(false);
    }
  }
}
```

## Adding New Features

To add a new feature (e.g., "properties"):

1. **Create feature folder structure**:
   ```bash
   mkdir -p src/app/features/properties/{models,services,pages,components}
   ```

2. **Create models**: `models/property.model.ts`

3. **Create service**: `services/properties.service.ts`

4. **Create pages**: `pages/properties-list/`, `pages/property-detail/`

5. **Create routes**: `routes.ts`

6. **Create barrel export**: `index.ts`

7. **Add to app routes**: Update `app.routes.ts`

## Best Practices

### Standalone Components
All components use the standalone API:
```typescript
@Component({
  selector: 'app-example',
  imports: [CommonModule, ButtonModule],  // Direct imports
  // No 'standalone: true' needed in Angular 21+
})
```

### Modern Control Flow
Use Angular's built-in control flow:
```html
@if (condition) {
  <div>Content</div>
}

@for (item of items(); track item.id) {
  <div>{{ item.name }}</div>
}
```

### Type Safety
- Define explicit interfaces for all models
- Use TypeScript strict mode
- Type all service methods and component properties

### Code Organization
- Keep files small and focused
- One component per file
- Co-locate related files (component + template + styles)
- Use barrel exports for clean imports

## Development Workflow

1. **Start the development server**:
   ```bash
   npm start
   ```

2. **Run with specific configuration**:
   ```bash
   npm start -- --configuration=development
   ```

3. **Build for production**:
   ```bash
   npm run build
   ```

## Future Enhancements

- [ ] Add error interceptor for global error handling
- [ ] Add loading interceptor for global loading state
- [ ] Add confirmation dialog service
- [ ] Add toast notification service
- [ ] Add form validation utilities
- [ ] Add organization form (create/edit) component
- [ ] Add more feature modules (properties, tenants, leases, financials)
