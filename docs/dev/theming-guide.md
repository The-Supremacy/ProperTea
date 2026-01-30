# ProperTea Theming Guide

**All-in-one guide for styling, colors, and theming in the ProperTea Angular app**

---

## Quick Rules

### ✅ DO
- Use **pure PrimeNG components** without color classes: `<p-button label="Save" />`
- Use **Tailwind with PrimeNG tokens** for custom HTML: `<div class="bg-surface-card text-color">`
- Use **Tailwind utilities** for layout: `flex`, `gap-4`, `p-4`
- Always include **dark mode variants** for non-theme colors: `text-orange-600 dark:text-orange-400`

### ❌ DON'T
- Add Tailwind color classes to PrimeNG components: ~~`<p-button class="bg-primary" />`~~
- Use raw Tailwind colors without dark variants: ~~`text-blue-500`~~
- Use inline styles for colors: ~~`style="background-color: #10b981;"`~~
- Change `surface` palette to brand colors (it's used for text/borders/backgrounds)

---

## The Pattern

### PrimeNG Components (No Custom Classes)

```html
<!-- Buttons styled by theme automatically -->
<p-button label="Save" />
<p-button label="Cancel" severity="secondary" [outlined]="true" />
<p-button label="Delete" severity="danger" />

<!-- Inputs automatically themed -->
<input pInputText formControlName="name" />

<!-- Cards automatically themed -->
<p-card>
  <ng-template pTemplate="header">Header</ng-template>
  Content here
</p-card>

<!-- Avatars automatically themed -->
<p-avatar label="JD" shape="circle" />
```

### Custom HTML Elements (Use Tailwind with PrimeNG Tokens)

```html
<!-- Icons -->
<i class="pi pi-user text-primary"></i>
<i class="pi pi-check text-green-600 dark:text-green-400"></i>

<!-- Custom cards/containers -->
<div class="bg-surface-card rounded-border p-6">
  <h3 class="text-color text-xl font-semibold">Title</h3>
  <p class="text-muted-color">Description</p>
</div>

<!-- Custom badges (non-PrimeNG) -->
<span class="bg-primary/10 text-primary px-2 py-1 rounded text-sm">
  Active
</span>
```

### Layout (Pure Tailwind)

```html
<div class="flex items-center justify-between gap-4 p-4">
  <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
    <!-- Content -->
  </div>
</div>
```

---

## Available Theme Tokens (via Tailwind)

The `tailwindcss-primeui` plugin generates these classes from your ProperTeaPreset:

### Colors
- `bg-primary`, `text-primary`, `border-primary` - Violet brand color
- `text-color` - Main text color (slate.700 light, zinc.300 dark)
- `text-muted-color` - Secondary text (slate.500)
- `bg-surface-card` - Card backgrounds (white light, zinc.800 dark)
- `bg-surface-ground` - Page backgrounds (lightest)

### Spacing & Borders
- `rounded-border` - Consistent border radius from theme
- `gap-*`, `p-*`, `m-*` - Standard Tailwind spacing

---

## How It Works

### Your ProperTeaPreset

Located at: `/src/app/theme/propertea.preset.ts`

```typescript
import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

export const ProperTeaPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{violet.50}',
      // ... violet palette
      600: '{violet.600}', // Used in light mode
      500: '{violet.500}', // Used in dark mode
    },
  },
  components: {
    // Component-specific overrides (if needed)
  },
});
```

**Key points:**
- `primary`: Your brand color (violet)
- `surface`: Neutral grays (slate light, zinc dark) - DON'T change to brand colors
- Extends `Aura` preset - inherits all semantic tokens

### CSS Variables Flow

```
ProperTeaPreset → PrimeNG CSS Variables → Tailwind Classes
                     ↓
              :root {
                --p-primary-color: violet;
                --p-text-color: slate.700;
              }
                     ↓
         .bg-primary { background: var(--p-primary-color); }
         .text-color { color: var(--p-text-color); }
```

### Why Surface Must Be Neutral

The `surface` palette is referenced EVERYWHERE for neutral elements:

```typescript
// Inherited from Aura:
text: {
  color: '{surface.700}',        // Main text
  mutedColor: '{surface.500}',   // Secondary text
}
content: {
  background: '{surface.0}',     // Card backgrounds
  borderColor: '{surface.200}',  // Borders
}
```

**If you change surface to violet, text/borders/backgrounds all turn violet!**

---

## Color Scheme Rationale

### Why Violet?

- **Low eye strain**: Mid-saturation purple is easier on eyes than bright blues
- **Professional**: Used by enterprise tools (Stripe, Linear, Notion)
- **Distinctive**: Not overused in property management software
- **Accessible**: Good contrast ratios in both light/dark modes

### The Palette

- **Primary**: Violet (600 light, 500 dark) - buttons, links, highlights
- **Surface**: Slate (light) / Zinc (dark) - backgrounds, text, borders
- **Accents**: Use contextual Tailwind colors with dark variants:
  - Success: `text-green-600 dark:text-green-400`
  - Warning: `text-orange-600 dark:text-orange-400`
  - Error: `text-red-600 dark:text-red-400`

### Changing Colors

To use different brand colors, edit `/src/app/theme/propertea.preset.ts`:

```typescript
semantic: {
  primary: {
    50: '{blue.50}',   // Change to your color
    100: '{blue.100}',
    // ... all shades 50-950
  }
}
```

Keep `surface` neutral (slate/zinc) for text and backgrounds.

---

## Dynamic Organization Branding

**Future requirement**: Each organization (from ZITADEL) can have custom branding.

### Strategy

Use PrimeNG's `updatePrimaryPalette()` to dynamically change the primary color at runtime:

```typescript
// auth.service.ts or theme.service.ts
import { updatePrimaryPalette } from '@primeuix/themes';

class ThemeService {
  applyOrgBranding(orgPrimaryColor: string) {
    // orgPrimaryColor from ZITADEL org metadata (e.g., '#3b82f6')
    updatePrimaryPalette(orgPrimaryColor);
  }
}
```

### Implementation Steps (when needed)

1. **Store org branding** in ZITADEL org metadata:
   ```json
   {
     "primaryColor": "#3b82f6",
     "logo": "https://cdn.example.com/org-logo.png"
   }
   ```

2. **Retrieve on login**:
   ```typescript
   async onLogin(orgId: string) {
     const orgSettings = await this.zitadelService.getOrgMetadata(orgId);
     this.themeService.applyOrgBranding(orgSettings.primaryColor);
   }
   ```

3. **Apply theme**:
   ```typescript
   applyOrgBranding(color: string) {
     updatePrimaryPalette(color);
     // All PrimeNG components and Tailwind classes update automatically
   }
   ```

4. **Reset on logout**:
   ```typescript
   onLogout() {
     updatePrimaryPalette('#8b5cf6'); // Back to default violet
   }
   ```

**Benefits:**
- No component changes needed
- Tailwind classes (`bg-primary`) update automatically
- Dark mode continues working
- Per-org isolation maintained

---

## Common Gotchas

### ❌ Forgetting Dark Mode Variants

```html
<!-- BAD: Only works in light mode -->
<span class="text-orange-500">Warning</span>

<!-- GOOD: Works in both modes -->
<span class="text-orange-600 dark:text-orange-400">Warning</span>
```

### ❌ Adding Classes to PrimeNG Components

```html
<!-- BAD: Unnecessary, theme handles it -->
<p-button [class]="'bg-primary text-white'" />

<!-- GOOD: Let PrimeNG style it -->
<p-button label="Save" />
```

### ❌ Using Inline Styles for Colors

```html
<!-- BAD: Hardcoded, no dark mode -->
<div style="background-color: #10b981;">...</div>

<!-- GOOD: Theme-aware -->
<div class="bg-primary">...</div>
```

### ❌ Mixing Theme and Arbitrary Colors

```html
<!-- BAD: Inconsistent -->
<div class="bg-surface-card border border-purple-500">...</div>

<!-- GOOD: Consistent with theme -->
<div class="bg-surface-card border border-surface">...</div>

<!-- OR: Explicit with dark mode -->
<div class="bg-surface-card border border-purple-600 dark:border-purple-400">
```

### ✅ Correct Pattern

```html
<!-- PrimeNG components: pure -->
<p-button label="Save" severity="primary" />

<!-- Custom HTML: Tailwind with theme tokens -->
<div class="bg-surface-card text-color p-4">
  <i class="pi pi-check text-primary"></i>
</div>

<!-- Accent colors: include dark variants -->
<span class="text-orange-600 dark:text-orange-400">Warning</span>
```

---

## File Locations

- **Theme preset**: `/src/app/theme/propertea.preset.ts`
- **App config**: `/src/app/app.config.ts` (providePrimeNG with cssLayer)
- **Tailwind config**: `/src/tailwind.css` (`@import "tailwindcss-primeui"`)

---

## Further Reading

- [PrimeNG Theming Docs](https://primeng.org/theming)
- [tailwindcss-primeui Plugin](https://github.com/primefaces/tailwindcss-primeui)
- `/docs/architecture.md` - Overall architecture
