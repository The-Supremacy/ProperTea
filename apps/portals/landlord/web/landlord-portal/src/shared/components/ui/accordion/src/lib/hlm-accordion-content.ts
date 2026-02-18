import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BrnAccordionContent } from '@spartan-ng/brain/accordion';
import { classes } from '@spartan-ng/helm/utils';

@Component({
	selector: 'hlm-accordion-content',
	changeDetection: ChangeDetectionStrategy.OnPush,
	hostDirectives: [{ directive: BrnAccordionContent, inputs: ['style'] }],
	template: `
		<div class="flex flex-col pt-0 pb-4">
			<ng-content />
		</div>
	`,
})
export class HlmAccordionContent {
	constructor() {
		classes(
			() => 'text-sm transition-all data-[state=closed]:h-0 data-[state=closed]:overflow-hidden data-[state=open]:h-auto! data-[state=open]:overflow-visible!',
		);
	}
}
