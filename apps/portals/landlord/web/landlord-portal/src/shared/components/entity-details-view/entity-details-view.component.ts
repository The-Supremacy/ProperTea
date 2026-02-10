import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  inject,
} from '@angular/core';
import { Location } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';
import { OverlayModule } from '@angular/cdk/overlay';

import { EntityDetailsConfig, EntityDetailsAction } from './entity-details-view.models';
import { ButtonDirective } from '../button';
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
    OverlayModule,
    TranslocoPipe,
    ButtonDirective,
    IconComponent,
  ],
  templateUrl: './entity-details-view.component.html',
  styleUrl: './entity-details-view.component.css',
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

  // State
  protected actionsMenuOpen = signal(false);

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
