# Core Module

App-wide singleton services, guards, and interceptors. These are configured once in `app.config.ts` and run for the entire application lifecycle.

## Structure

- **guards/** - Route guards (auth, unsaved changes)
- **interceptors/** - HTTP interceptors (correlation ID, tenant context, error handling)
- **models/** - Shared TypeScript interfaces and types
- **services/** - Core singleton services (config, session, theme, error handling)

## Guidelines

- Core services should be stateless or maintain app-wide state only
- Use `providedIn: 'root'` for all core services
- Interceptors are registered via `provideHttpClient(withInterceptors([...]))`
- Guards are applied in route definitions
- Never import core services directly into shared components
