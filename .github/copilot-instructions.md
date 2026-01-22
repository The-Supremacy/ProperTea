# General
- This is a .NET Aspire monorepo. The Source of Truth for Models is in /shared/contracts.
- We use a BFF pattern. Do not put business logic in the BFF; it is a pass-through/mapper only.
- Frontend is Next.js/Angular. Use Tailwind for styling.

# Documentation
- All available project documentation is in /docs.
- Use proper markdown syntax for all documentation.
- Do not create XML documentation for C# code members unless they describe something very specific (no 'Creates a user').
- No emojis.

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
