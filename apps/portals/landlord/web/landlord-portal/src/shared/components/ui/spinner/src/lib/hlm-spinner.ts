import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideLoader } from '@ng-icons/lucide';
import { classes } from '@spartan-ng/helm/utils';

@Component({
	selector: 'hlm-spinner',
	imports: [NgIcon],
	providers: [provideIcons({ lucideLoader })],
	changeDetection: ChangeDetectionStrategy.OnPush,
	host: {
		role: 'status',
		'[attr.aria-label]': 'ariaLabel()',
	},
	template: `
		<ng-icon [name]="icon()" />
	`,
})
export class HlmSpinner {
	/**
	 * The name of the icon to be used as the spinner.
	 * Use provideIcons({ ... }) to register custom icons.
	 */
	public readonly icon = input<string>('lucideLoader');

	/** Aria label for the spinner for accessibility. */
	public readonly ariaLabel = input<string>('Loading', { alias: 'aria-label' });

	/** Size of the spinner. sm = inline text, lg = full-page loader. */
	public readonly size = input<'sm' | 'md' | 'lg'>('sm');

	constructor() {
		const sizeClass: Record<string, string> = { sm: 'text-base', md: 'text-2xl', lg: 'text-5xl' };
		classes(() => `inline-flex size-fit ${sizeClass[this.size()]} motion-safe:animate-spin`);
	}
}
