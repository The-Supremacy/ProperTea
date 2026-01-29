import { Component, input } from '@angular/core';

/**
 * Page header component with responsive title and optional subtitle
 * Used consistently across documentation and content pages
 */
@Component({
  selector: 'app-page-header',
  standalone: true,
  template: `
    <header class="mb-6">
      <ng-content select="[actions]"></ng-content>
      <h1 class="text-3xl sm:text-4xl lg:text-5xl font-bold mb-3 text-surface-900 dark:text-surface-0">
        <ng-content select="[title]"></ng-content>
      </h1>
      @if (hasSubtitle()) {
        <p class="text-base sm:text-lg lg:text-xl text-surface-600 dark:text-surface-400">
          <ng-content select="[subtitle]"></ng-content>
        </p>
      }
    </header>
  `
})
export class PageHeaderComponent {
  hasSubtitle = input<boolean>(true);
}
