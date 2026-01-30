# General
- This is a .NET Aspire monorepo. The Source of Truth for Models is in /shared/contracts.
- We use a BFF pattern. Do not put business logic in the BFF; it is a pass-through/mapper only.
- Frontend is Next.js/Angular. Use Tailwind for styling.

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
- **State Management**: Use Signals for local component state. Use Services with Signals for shared state.
- **Forms**: Use Reactive Forms only.
- **Styling**:
    - **PrimeNG Components**: Use pure PrimeNG styling—NO Tailwind color classes on components like p-button, p-card, p-avatar, etc.
    - **Custom Elements**: Use Tailwind with PrimeNG tokens for custom HTML: `bg-surface-card`, `text-color`, `bg-primary`
    - **Layout**: Use Tailwind utilities: `flex`, `gap-4`, `p-4`, `w-full`
    - **Dark Mode**: Always include dark variants for non-theme colors: `text-orange-600 dark:text-orange-400`
    - See /docs/dev/theming-guide.md for complete strategy
- **Internationalization**: Use Transloco for all user-facing text. Do not hardcode strings.
- **Components**:
    - Follow Atomic Design principles. Keep components small and focused.
    - Create components split into html, ts, and css/scss files.
