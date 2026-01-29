import { Component, inject, effect } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { BreadcrumbService } from '../../../core/services/breadcrumb.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  standalone: true,
  imports: [TranslocoModule]
})
export class DashboardComponent {
  private readonly breadcrumbService = inject(BreadcrumbService);

  constructor() {
    effect(() => {
      this.breadcrumbService.clear();
    });
  }
}
