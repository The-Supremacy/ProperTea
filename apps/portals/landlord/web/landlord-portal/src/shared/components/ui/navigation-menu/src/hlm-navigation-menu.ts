import { Directive } from '@angular/core';
import { BrnNavigationMenu } from '@spartan-ng/brain/navigation-menu';
import { classes } from '@spartan-ng/helm/utils';

@Directive({
	selector: 'nav[hlmNavigationMenu]',
	hostDirectives: [
		{
			directive: BrnNavigationMenu,
			inputs: ['value', 'delayDuration', 'skipDelayDuration', 'orientation', 'openOn'],
			outputs: ['valueChange'],
		},
	],
})
export class HlmNavigationMenu {
	constructor() {
		classes(
			() =>
				'group/navigation-menu relative flex data-[orientation=horizontal]:max-w-max data-[orientation=horizontal]:flex-1 data-[orientation=horizontal]:items-center data-[orientation=horizontal]:justify-center data-[orientation=vertical]:flex-col data-[orientation=vertical]:h-full',
		);
	}
}
