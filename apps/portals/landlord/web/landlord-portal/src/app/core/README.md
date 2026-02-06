# Core Module

App-wide singleton services and guards. These are configured once in `app.config.ts` and run for the entire application lifecycle.

## Structure

- **guards/** - Route guards (auth, unsaved changes)
- **services/** - Core singleton services (config, session, theme, error handling)

## Guidelines

- Core services should be stateless or maintain app-wide state only
- Use `providedIn: 'root'` for all core services
- Guards are applied in route definitions
- Never import core services directly into shared components
