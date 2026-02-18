import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  inject,
} from '@angular/core';
import { Location } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';

import { EntityDetailsConfig, EntityDetailsAction } from './entity-details-view.models';
import { HlmButton } from '@spartan-ng/helm/button';
import { IconComponent } from '../icon';
import { ResponsiveService } from '../../../app/core/services/responsive.service';

/**
 * Generic entity details view wrapper component.
 * Provides consistent toolbar with back button, refresh, actions menu, and custom content area.
 *
 * @example
 * ```typescript
 * <app-entity-details-view
 *   [config]="detailsConfig()"
 *   (refresh)="loadData()">
 *   <!-- Custom form/content goes here -->
 *   <form>...</form>
 * </app-entity-details-view>
 * ```
 */
@Component({
  selector: 'app-entity-details-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    HlmDropdownMenuImports,
    TranslocoPipe,
    HlmButton,
    IconComponent,
  ],
  templateUrl: './entity-details-view.component.html',
})
export class EntityDetailsViewComponent {
  // Services
  private location = inject(Location);
  protected responsive = inject(ResponsiveService);

  // Inputs
  config = input.required<EntityDetailsConfig>();
  loading = input<boolean>(false);

  // Outputs
  refresh = output<void>();

  protected goBack(): void {
    this.location.back();
  }

  protected onRefresh(): void {
    this.refresh.emit();
  }

  protected async handleAction(action: EntityDetailsAction): Promise<void> {
    if (action.disabled) return;
    await action.handler();
  }

  protected trackByLabel(_index: number, action: EntityDetailsAction): string {
    return action.label;
  }
}
