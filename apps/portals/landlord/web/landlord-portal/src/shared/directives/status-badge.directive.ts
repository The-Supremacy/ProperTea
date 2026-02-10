import { Directive, input, computed, HostBinding } from '@angular/core';

export type StatusVariant = 'active' | 'inactive' | 'pending' | 'default';

@Directive({
  selector: '[appStatusBadge]'
})
export class StatusBadgeDirective {
  status = input.required<string>({ alias: 'appStatusBadge' });
  variant = input<StatusVariant | undefined>(undefined);

  private statusVariant = computed(() => {
    // Allow explicit variant override
    if (this.variant()) {
      return this.variant();
    }

    // Auto-detect variant from status string (case-insensitive)
    const statusLower = this.status().toLowerCase();
    if (statusLower === 'active') return 'active';
    if (statusLower === 'inactive' || statusLower === 'deactivated' || statusLower === 'deleted') return 'inactive';
    if (statusLower === 'pending') return 'pending';
    return 'default';
  });

  @HostBinding('class')
  get hostClasses() {
    const variant = this.statusVariant() ?? 'default';
    const baseClasses = 'inline-flex rounded-full px-3 py-1.5 text-sm font-semibold';

    const variantClasses: Record<StatusVariant, string> = {
      active: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
      inactive: 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-200',
      pending: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200',
      default: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'
    };

    return `${baseClasses} ${variantClasses[variant]}`;
  }
}
