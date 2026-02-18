import { Directive } from '@angular/core';
import { BrnNavigationMenuLink } from '@spartan-ng/brain/navigation-menu';
import { classes } from '@spartan-ng/helm/utils';

@Directive({
	selector: 'a[hlmNavigationMenuLink]',
	hostDirectives: [{ directive: BrnNavigationMenuLink, inputs: ['active'] }],
})
export class HlmNavigationMenuLink {
	constructor() {
		classes(() => [
			'data-[active=true]:focus:bg-accent data-[active=true]:hover:bg-accent data-[active=true]:bg-accent/50 data-[active=true]:text-accent-foreground hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground focus-visible:ring-ring/50 text-sm transition-all outline-none focus-visible:ring-[3px] focus-visible:outline-1',
			// Horizontal (mega-menu): stacked layout with icon + title + description
			'group-data-[orientation=horizontal]/navigation-menu:flex group-data-[orientation=horizontal]/navigation-menu:flex-col group-data-[orientation=horizontal]/navigation-menu:gap-1 group-data-[orientation=horizontal]/navigation-menu:rounded-sm group-data-[orientation=horizontal]/navigation-menu:p-2 [&_ng-icon:not([class*="text-"])]:text-base group-data-[orientation=horizontal]/navigation-menu:[&_ng-icon:not([class*="text-"])]:text-muted-foreground',
			// Vertical (sidebar): row layout with icon + label side by side
			'group-data-[orientation=vertical]/navigation-menu:flex group-data-[orientation=vertical]/navigation-menu:flex-row group-data-[orientation=vertical]/navigation-menu:items-center group-data-[orientation=vertical]/navigation-menu:gap-3 group-data-[orientation=vertical]/navigation-menu:rounded-md group-data-[orientation=vertical]/navigation-menu:px-3 group-data-[orientation=vertical]/navigation-menu:py-2 group-data-[orientation=vertical]/navigation-menu:w-full',
			'data-[disabled=true]:pointer-events-none data-[disabled=true]:opacity-50',
		]);
	}
}
