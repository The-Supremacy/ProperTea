# General
- This is a .NET Aspire monorepo. The Source of Truth for Models is in /shared/contracts.
- We use a BFF pattern. Do not put business logic in the BFF; it is a pass-through/mapper only.
- Frontend is Angular. Use Tailwind for all styling.

# Documentation
- All available project documentation is in /docs.
- Development guides and quirky behavior patterns are in /docs/dev.
- Use proper markdown syntax for all documentation.
- Do not create XML documentation for C# code members unless they describe something very specific (no 'Creates a user').
- No emojis (except in /docs/dev for clarity).

# Technical Stack & Patterns
- **Messaging**: Wolverine. Handlers must implement `IWolverineHandler`.
- **Persistence**: Marten. Use `IDocumentSession` for data access.
- **Event Sourcing**:
  - Aggregates implement `IRevisioned`.
  - Events are immutable records defined in a static `Events` class.
  - Apply methods (e.g., `Apply(Created e)`) must be inside the Aggregate class.
  - Aggregates implement Decider pattern inside: The provide a domain method (e.g., `Register(...)`) that returns events, and an Apply method for each event to mutate state.
- **Transactions**: Wolverine manages transactions automatically via `AutoApplyTransactions()`. Do not manually call `SaveChanges` unless strictly necessary for immediate read-after-write logic.
- **Integration**:
  - Use `[MessageIdentity("...")]` on integration events.
  - Shared contracts reside in `ProperTea.Contracts`.

# Angular guidelines
You are an expert in TypeScript, Angular, and scalable web application development. You write functional, maintainable, performant, and accessible code following Angular and TypeScript best practices.

## UI Component Strategy (Headless-First)
Follow this priority when choosing components:
1. **Angular Aria** - For: Select, Autocomplete, Menu, Tabs, Tree, Accordion, Listbox, Grid, Toolbar
2. **Angular Material** - For: Dialog, Snackbar (Toast), Date Picker, Slider, Tooltip, Progress Spinner/Bar
3. **Pure Tailwind** - For: Button, Input, Badge, Card, simple layouts, Drawer
4. **Angular CDK** - For: Drag/drop, Virtual scroll, Clipboard, Platform detection, Overlay

## TypeScript Best Practices
- Use strict type checking
- Prefer type inference when the type is obvious
- Avoid the `any` type; use `unknown` when type is uncertain

## Angular Best Practices
- Always use standalone components over NgModules
- Must NOT set `standalone: true` inside Angular decorators. It's the default in Angular v20+.
- Use signals for state management
- Implement lazy loading for feature routes
- Do NOT use the `@HostBinding` and `@HostListener` decorators. Put host bindings inside the `host` object of the `@Component` or `@Directive` decorator instead
- Use `NgOptimizedImage` for all static images.
  - `NgOptimizedImage` does not work for inline base64 images.

## Accessibility Requirements
- It MUST pass all AXE checks.
- It MUST follow all WCAG AA minimums, including focus management, color contrast, and ARIA attributes.

### Components
- Keep components small and focused on a single responsibility
- Use `input()` and `output()` functions instead of decorators
- Use `computed()` for derived state
- Set `changeDetection: ChangeDetectionStrategy.OnPush` in `@Component` decorator
- Prefer inline templates for small components
- Prefer Reactive forms instead of Template-driven ones
- Do NOT use `ngClass`, use `class` bindings instead
- Do NOT use `ngStyle`, use `style` bindings instead
- When using external templates/styles, use paths relative to the component TS file.

## State Management
- Use signals for local component state
- Use `computed()` for derived state
- Keep state transformations pure and predictable
- Do NOT use `mutate` on signals, use `update` or `set` instead

## Templates
- Keep templates simple and avoid complex logic
- Use native control flow (`@if`, `@for`, `@switch`) instead of `*ngIf`, `*ngFor`, `*ngSwitch`
- Use the async pipe to handle observables
- Do not assume globals like (`new Date()`) are available.
- Do not write arrow functions in templates (they are not supported).

## Services
- Design services around a single responsibility
- Use the `providedIn: 'root'` option for singleton services
- Use the `inject()` function instead of constructor injection
