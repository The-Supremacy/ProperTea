import { computed, Directive, input } from '@angular/core';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { classes } from '@spartan-ng/helm/utils';

export type StatusVariant = 'active' | 'inactive' | 'pending' | 'default';

const STATUS_CLASSES: Record<StatusVariant, string> = {
  active: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200 border-green-200 dark:border-green-800',
  inactive: 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-200 border-gray-200 dark:border-gray-700',
  pending: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200 border-yellow-200 dark:border-yellow-800',
  default: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200 border-blue-200 dark:border-blue-800',
};

@Directive({
  selector: '[appStatusBadge]',
  hostDirectives: [HlmBadge],
})
export class StatusBadgeDirective {
  readonly status = input.required<string>({ alias: 'appStatusBadge' });
  readonly variant = input<StatusVariant | undefined>(undefined);

  private readonly resolvedVariant = computed<StatusVariant>(() => {
    if (this.variant()) return this.variant()!;
    const s = this.status().toLowerCase();
    if (s === 'active') return 'active';
    if (s === 'inactive' || s === 'deactivated' || s === 'deleted') return 'inactive';
    if (s === 'pending') return 'pending';
    return 'default';
  });

  constructor() {
    classes(() => STATUS_CLASSES[this.resolvedVariant()]);
  }
}
