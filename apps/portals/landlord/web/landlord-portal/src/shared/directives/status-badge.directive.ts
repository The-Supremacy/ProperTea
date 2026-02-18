import { computed, Directive, input } from '@angular/core';
import { hlm } from '@spartan-ng/helm/utils';

export type StatusVariant = 'active' | 'inactive' | 'pending' | 'default';

const STATUS_CLASSES: Record<StatusVariant, string> = {
  active: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
  inactive: 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-200',
  pending: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200',
  default: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200',
};

@Directive({
  selector: '[appStatusBadge]',
  host: {
    '[class]': 'statusClasses()',
  },
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

  protected readonly statusClasses = computed(() =>
    hlm(
      'inline-flex items-center rounded-full px-3 py-1.5 text-xs font-semibold whitespace-nowrap',
      STATUS_CLASSES[this.resolvedVariant()],
    ),
  );
}
