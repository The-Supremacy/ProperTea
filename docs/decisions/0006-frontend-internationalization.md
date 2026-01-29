# Frontend Internationalization Strategy

**Status**: Accepted
**Date**: 2026-01-27
**Deciders**: Development Team
**Updated**: 2026-01-27 (Switched from @angular/localize to Transloco)

## Context

The ProperTea Landlord Portal requires internationalization (i18n) support to accommodate users in multiple languages, starting with English and Ukrainian. Key requirements include:

- Support for runtime language switching
- Minimal external dependencies (prefer official or well-maintained solutions)
- Integration with PWA architecture
- Backend services use event codes (e.g., `ORG_CREATED`) rather than translated messages
- Frontend is responsible for translating event codes to user's language
- Simple translation workflow (no build-time complexity)
- Single build artifact for all locales

We evaluated three primary options:
1. **@angular/localize** - Official Angular i18n solution
2. **ngx-translate/core** - Popular community library
3. **Transloco** - Modern alternative with premium features

## Decision

We will use **Transloco** (@jsverse/transloco) for frontend internationalization.

### Implementation Approach

**Translation Architecture:**
```
Backend Services:
├─ Logs: English only (developer-facing)
├─ Audit Events: Event codes (ORG_CREATED, USER_INVITED)
└─ Error Responses: Error codes + context

Frontend Application:
├─ Translates event codes → User's language
├─ Translates UI strings
├─ Runtime language switching
└─ No translation sync with backend needed
```

**File Structure:**
```
src/
├─ assets/
│  └─ i18n/
│     ├─ en.json (English)
│     └─ uk.json (Ukrainian)
├─ app/
│  ├─ transloco-loader.ts (HTTP loader for JSON files)
│  └─ app.config.ts (Transloco provider configuration)
```

**Usage Examples:**
```typescript
// Template syntax
<h1>{{ 'dashboard.title' | transloco }}</h1>
<p-button [label]="'button.save' | transloco" />

// TypeScript (with TranslocoService)
constructor(private translocoService: TranslocoService) {}

const message = this.translocoService.translate('error.not_found');

// Switch language at runtime
this.translocoService.setActiveLang('uk');

// Event code translation
eventCode = 'ORG_CREATED';
message = this.translocoService.translate(`events.${eventCode}`);
```

## Consequences

### Positive

* **Runtime Language Switching**: Instant language change without page reload
* **Single Build**: One build artifact serves all locales (simplified deployment)
* **Modern Architecture**: Built for Angular 14+, signals support coming
* **JSON Translation Files**: Easy to edit, no XML complexity
* **Active Maintenance**: Maintained by Nx/Nrwl team
* **Lazy Loading**: Can scope translations by feature module
* **Backend Independence**: Event codes eliminate need for backend translations
* **Type Safety**: TypeScript autocomplete with keys-manager
* **Simple Workflow**: Add keys to JSON, no extraction step needed
* **PWA Compatible**: Works seamlessly with service workers
* **Security**: Translations loaded via HTTP, same as other assets

### Negative

* **Third-Party Dependency**: Not official Angular (but well-maintained by Nx team)
* **Smaller Community**: Less Stack Overflow coverage than ngx-translate
* **Bundle Size**: ~30kb overhead (marginal compared to PrimeNG)
* **Runtime Overhead**: Translation lookup at runtime (negligible with modern Angular)

### Risks / Mitigation

* **Risk**: Library abandonment by Nx team
  * **Mitigation**: Nx is Google-backed, stable for 5+ years; open source allows forking
  * **Mitigation**: Migration to ngx-translate is straightforward (similar APIs)

* **Risk**: Developers forget to mark strings as translatable
  * **Mitigation**: Add linting rules to detect hard-coded strings
  * **Mitigation**: Code review checklist includes i18n verification

* **Risk**: Translation files get out of sync
  * **Mitigation**: JSON files are easier to maintain than XLF
  * **Mitigation**: Keys manager can validate missing translations

## Alternatives Considered

### @angular/localize (Previously Used)
- **Pros**: Official Angular, type-safe, bundle optimization
- **Cons**: Requires separate build per locale, no runtime switching, complex XLF workflow, difficult deployment
- **Rejected**: Build-time translations created deployment complexity; separate builds for each locale is overhead we don't need

### ngx-translate/core
- **Pros**: Runtime switching, simpler API, large community, battle-tested
- **Cons**: Third-party dependency, 10+ years old, not optimized for modern Angular (no standalone/signals support)
- **Rejected**: While stable, Transloco is more aligned with modern Angular patterns

### No i18n (English only)
- **Pros**: Simplest, no overhead
- **Cons**: Limits market expansion, especially in EU
- **Rejected**: Multilingual support is essential for European market

## Notes

- MVP launches with English (en) and Ukrainian (uk)
- Translation infrastructure supports runtime language switching
- Backend event codes ensure no translation duplication
- Audit logs remain machine-readable (codes, not translated text)
- Single build artifact simplifies deployment
- JSON translation files are easy to edit and version control
