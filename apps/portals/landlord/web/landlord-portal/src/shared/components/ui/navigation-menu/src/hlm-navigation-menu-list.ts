import { Directive } from '@angular/core';
import { BrnNavigationMenuList } from '@spartan-ng/brain/navigation-menu';
import { classes } from '@spartan-ng/helm/utils';

@Directive({
	selector: 'ul[hlmNavigationMenuList]',
	hostDirectives: [
		{
			directive: BrnNavigationMenuList,
		},
	],
})
export class HlmNavigationMenuList {
	constructor() {
		classes(() => [
			'group flex flex-1 list-none gap-1',
			'data-[orientation=horizontal]:items-center data-[orientation=horizontal]:justify-center',
			'data-[orientation=vertical]:flex-col data-[orientation=vertical]:items-stretch data-[orientation=vertical]:w-full data-[orientation=vertical]:p-2',
		]);
	}
}
