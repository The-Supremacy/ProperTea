---
applyTo: "**/landlord-portal/**"
---

# Angular Development (Landlord Portal)

Stack: Angular 21+, Tailwind CSS 4, Vitest, Zoneless change detection, PWA.

## Component Rules
- Standalone components only. Do NOT set `standalone: true` (it's the default in Angular 21+).
- Always set `changeDetection: ChangeDetectionStrategy.OnPush`.
- Use `input()` / `output()` functions, not `@Input` / `@Output` decorators.
- Use `computed()` for derived state, never getter functions.
- Use `host` object in `@Component`/`@Directive` for host bindings. Never `@HostBinding` or `@HostListener`.
- Prefer inline templates for small components. Use relative paths for external templates.
- Use `inject()`, not constructor injection.

## UI Component Strategy (priority order)
1. **Angular Aria** (`@angular/aria`) - Select, Autocomplete, Menu, Tabs, Tree, Accordion, Listbox, Grid, Toolbar.
2. **Angular Material** - Dialog, Snackbar, Date Picker, Slider, Tooltip, Progress indicators.
3. **Pure Tailwind** - Button, Input, Badge, Card, simple layouts, Drawer.
4. **Angular CDK** - Drag/drop, Virtual scroll, Clipboard, Platform detection, Overlay.

Use CVA (class-variance-authority) + `cn()` utility (clsx + tailwind-merge) for variant-based styling.

## State & Templates
- Signals for local state. `computed()` for derived state. No `mutate`, use `update`/`set`.
- Native control flow: `@if`, `@for`, `@switch`. Never `*ngIf`, `*ngFor`, `*ngSwitch`.
- Never `ngClass`/`ngStyle`. Use `class`/`style` bindings.
- Reactive Forms only. No Template-driven forms.
- Use `async` pipe for observables. No arrow functions in templates.
- Use `NgOptimizedImage` for all static images (does not work for inline base64).

## Feature Structure
Each feature lives in `features/{name}/` with:
- `routes.ts` - Lazy-loaded route definitions.
- `models/` - TypeScript interfaces/types.
- `services/` - Feature-scoped services (use `providedIn: 'root'` for singletons).
- View components as subfolders (e.g., `list-view/`, `details/`, `create-drawer/`).

## Internationalization
- Use Transloco (`@jsverse/transloco`). All user-visible strings must be translatable.
- Translation files: `assets/i18n/{locale}.json`.

## Accessibility
- Must pass all AXE checks and WCAG AA minimums.
- Proper focus management, color contrast, and ARIA attributes on all interactive elements.

## TypeScript
- Strict type checking. Prefer type inference when obvious.
- Never use `any`. Use `unknown` when type is uncertain.
