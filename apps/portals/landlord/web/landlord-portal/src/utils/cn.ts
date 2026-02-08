import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

/**
 * Utility function to merge Tailwind CSS classes with clsx.
 * Uses tailwind-merge to properly handle conflicting classes.
 *
 * @example
 * ```typescript
 * cn('px-2 py-1', true && 'bg-primary', { 'text-white': isActive })
 * // Returns merged class string
 * ```
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
