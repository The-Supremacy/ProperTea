import { Component, inject } from '@angular/core';
import { BreadcrumbModule } from 'primeng/breadcrumb';
import { BreadcrumbService } from '../../core/services/breadcrumb.service';

@Component({
  selector: 'app-breadcrumb',
  imports: [BreadcrumbModule],
  template: `
    @if (breadcrumbService.hasItems()) {
      <p-breadcrumb
        [model]="breadcrumbService.getBreadcrumbs()"
        [home]="{icon: 'pi pi-home', routerLink: '/'}"
        class="px-6 py-3 bg-surface-50 dark:bg-surface-900 border-bottom-1 border-surface"
      />
    }
  `,
  styles: `
    :host {
      display: block;
    }
  `
})
export class BreadcrumbComponent {
  protected readonly breadcrumbService = inject(BreadcrumbService);
}
