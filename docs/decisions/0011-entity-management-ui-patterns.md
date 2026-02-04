# ADR 0011: Entity Management UI Patterns

**Status**: Accepted
**Date**: 2026-01-31
**Context**: Establishing UI patterns for entity CRUD interfaces (starting with Companies)

---

## Context

Building the first domain entity CRUD interface (Companies). Patterns established here will apply to all future entities (Properties, Units, Tenants, etc.).

Requirements:
- Mobile-first responsive design
- Modern ERP-standard UX
- Support for complex aggregates
- Scalable to many columns/filters
- i18n from day one

---

## Decisions

### Navigation
Tailwind-styled sidebar with Angular CDK for collapse behavior. Feature-based grouping with Lucide icons.

### List View
TanStack Table with Spartan UI styling. Responsive design (mobile: card view, desktop: table). Checkbox column, data columns, actions menu (Spartan Dropdown).

### Column Visibility
3-tier system: Mandatory (always visible), Default (on by default), Optional (off by default). Spartan Sheet for selection, drag-to-reorder (CDK Drag), localStorage persistence.

### Save Strategy
Hybrid: Auto-save for simple fields (blur + 500ms debounce), explicit save for complex sections. Unsaved changes guard only for pending saves.

### Creation Flow
Navigate to `/{entities}/new`, auto-save on first required field blur, URL updates to `/{entities}/:id`.

### Complex Aggregates
Tabs for major sections + accordions within tabs. Mobile: tabs become swipeable.

### Filtering & Pagination
Server-side pagination with TanStack Table pagination. Collapsible filter panel (Spartan Accordion) with [Clear] [Apply] buttons. URL params for sharing. localStorage for preferences.

### Validation
Field-level inline errors using Spartan Form Field error states. Async uniqueness checks (debounced). Cross-field validation in explicit save sections.

### Delete
Spartan Alert Dialog confirmation. Backend blocks if entity has dependencies.

### Mobile
Responsive card layout for tables, Spartan Sheet for filters, bulk actions float at bottom with FAB.

### i18n
Two-tier: `common.*` (shared) + `{entity}.*` (specific). Keys generated just-in-time.

### Error Handling
RFC 7807 (ProblemDetails). Field errors inline, general errors as toasts.

### State Management
Angular Signals in entity service. Computed signals for derived state.

### Inter-Service Communication
Tier 1 (sync) for MVP. Tier 2 (polling) for complex workflows. Tier 3 (SignalR) future option.

### Real-Time Updates
Manual refresh only for MVP. Add polling/SignalR later if needed.

---

## Consequences

**Positive**: Consistent patterns, mobile-first, scalable, maintainable.

**Negative**: Initial complexity, learning curve for hybrid save.

**Risks**: Over-engineering (mitigate by starting minimal), mobile performance (use virtual scroll if needed).

---

## References

- [List View Pattern](/docs/dev/list-view-pattern.md)
- [Details View Pattern](/docs/dev/details-view-pattern.md)
- [Long-Running Process Patterns](/docs/dev/long-running-process-patterns.md)
- [RFC 7807: Problem Details](https://tools.ietf.org/html/rfc7807)
