# ADR 0012: Headless UI Component Strategy

**Status**: Accepted
**Date**: 2026-02-04
**Deciders**: Team

## Context

The initial frontend implementation used Angular Material for UI components. While Material is developed by the Angular team and provides good accessibility, several issues emerged:

1. **Tailwind Integration Friction**: Material's theming system conflicts with Tailwind's utility-first approach, requiring workarounds and CSS overrides.
2. **Component Limitations**: Material Table lacks features needed for complex data grids (virtual scrolling, column pinning, advanced filtering).
3. **Styling Inflexibility**: Material components have opinionated styling that's difficult to customize without fighting the framework.
4. **Bundle Size**: Material components include significant CSS that may not be needed.

The industry trend (evidenced by shadcn/ui's popularity in React) is moving toward headless components that separate behavior/accessibility from styling.

## Decision

Adopt a **headless-first component strategy** using:

### Priority System (when needing a component)

1. **Angular Aria** - Official Angular headless components with full a11y
2. **Angular CDK** - Low-level primitives when Aria doesn't cover it
3. **Spartan UI** - shadcn-style components for complex UI (Date Picker, Data Table, Dialog)
4. **Custom CDK** - Last resort for truly unique requirements

### Component Source Matrix

| Component | Source | Rationale |
|-----------|--------|-----------|
| Select, Autocomplete, Combobox | Angular Aria | Official, full a11y |
| Menu, Menubar | Angular Aria | Official, full a11y |
| Tabs, Accordion | Angular Aria | Official, full a11y |
| Tree, Grid | Angular Aria | Official, full a11y |
| Listbox, Multiselect | Angular Aria | Official, full a11y |
| Toolbar | Angular Aria | Official, full a11y |
| Button, Input, Textarea | Spartan (or custom Tailwind) | Simple primitives |
| Dialog, Sheet | Spartan | Complex overlay behavior |
| Date Picker, Calendar | Spartan | Complex date handling |
| Data Table | Spartan (wraps TanStack Table) | Advanced table features |
| Toast/Sonner | Spartan | Animation + stacking |
| Popover, Tooltip | Spartan | Positioning logic |
| Form Field, Label | Spartan | Form layout patterns |
| Progress, Spinner | Spartan | Visual feedback |
| Breadcrumb, Pagination | Spartan | Navigation patterns |

### Architecture

- **Angular Aria**: Headless directives that handle keyboard, ARIA, focus, screen readers. We provide HTML structure and Tailwind styling.
- **Spartan Brain**: Accessible primitives (like Aria but covering more components)
- **Spartan Helm**: Pre-styled Tailwind classes following shadcn design language
- **TanStack Table**: Headless table with sorting, filtering, pagination, virtual scroll

### Styling

Pure Tailwind CSS everywhere. No component library CSS imports. Spartan provides a Tailwind preset for consistent design tokens.

## Consequences

### Positive
- Full control over styling with Tailwind utilities
- No CSS conflicts or specificity battles
- Smaller bundle size (no unused Material CSS)
- Consistent shadcn-inspired design language
- Future-proof: can swap implementations without UI changes
- Better accessibility through purpose-built a11y libraries

### Negative
- Learning curve for new component patterns
- Need to build/configure more components upfront
- Spartan is community-maintained (not Google)
- Some components need manual aria-label additions

### Risks / Mitigation
- **Spartan abandonment** -> Core a11y from Angular Aria remains; could rebuild helm layer
- **Missing components** -> Angular CDK provides escape hatch; contribute to Spartan
- **Inconsistent styling** -> Enforce Tailwind preset + component documentation

## Supersedes

This decision updates the frontend stack choices in:
- ADR 0011 (Entity Management UI Patterns) - Component references
- Architecture documentation - Stack description
