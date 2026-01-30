import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

/**
 * ProperTea custom theme preset based on Aura.
 *
 * Color Scheme: Violet/Purple - Professional, calming, and easy on the eyes for long ERP sessions.
 * Violet (vs Purple) is warmer and less saturated, reducing eye strain during extended use.
 *
 * IMPORTANT: When customizing tokens that use colorScheme in the base preset,
 * you MUST also define them using colorScheme, otherwise your overrides will be ignored.
 *
 * The Aura preset defines most semantic tokens (primary, highlight, etc.) with colorScheme,
 * so we follow the same pattern to ensure our customizations take precedence.
 */
export const ProperTeaPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{violet.50}',
      100: '{violet.100}',
      200: '{violet.200}',
      300: '{violet.300}',
      400: '{violet.400}',
      500: '{violet.500}',
      600: '{violet.600}',
      700: '{violet.700}',
      800: '{violet.800}',
      900: '{violet.900}',
      950: '{violet.950}',
    },
    colorScheme: {
      light: {
        surface: {
          0: '#ffffff',
          50: '{slate.50}',
          100: '{slate.100}',
          200: '{slate.200}',
          300: '{slate.300}',
          400: '{slate.400}',
          500: '{slate.500}',
          600: '{slate.600}',
          700: '{slate.700}',
          800: '{slate.800}',
          900: '{slate.900}',
          950: '{slate.950}',
        },
        primary: {
          color: '{violet.600}',
          contrastColor: '#ffffff',
          hoverColor: '{violet.700}',
          activeColor: '{violet.800}',
        },
        highlight: {
          background: '{violet.50}',
          focusBackground: '{violet.100}',
          color: '{violet.700}',
          focusColor: '{violet.800}',
        },
      },
      dark: {
        surface: {
          0: '#ffffff',
          50: '{zinc.50}',
          100: '{zinc.100}',
          200: '{zinc.200}',
          300: '{zinc.300}',
          400: '{zinc.400}',
          500: '{zinc.500}',
          600: '{zinc.600}',
          700: '{zinc.700}',
          800: '{zinc.800}',
          900: '{zinc.900}',
          950: '{zinc.950}',
        },
        primary: {
          color: '{violet.500}',
          contrastColor: '{surface.900}',
          hoverColor: '{violet.400}',
          activeColor: '{violet.300}',
        },
        highlight: {
          background: 'color-mix(in srgb, {violet.400}, transparent 84%)',
          focusBackground: 'color-mix(in srgb, {violet.400}, transparent 76%)',
          color: 'rgba(255,255,255,.87)',
          focusColor: 'rgba(255,255,255,.87)',
        },
      },
    },
  },
});
