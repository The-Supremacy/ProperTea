import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog';
import { HlmButton } from '@spartan-ng/helm/button';
import {
  HlmAlertDialogDescription,
  HlmAlertDialogFooter,
  HlmAlertDialogHeader,
  HlmAlertDialogTitle,
} from '@spartan-ng/helm/alert-dialog';

export interface ConfirmDialogData {
  title: string;
  description: string;
  confirmText?: string;
  cancelText?: string;
  variant?: 'default' | 'destructive';
}

@Component({
  selector: 'app-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    HlmButton,
    TranslocoPipe,
    HlmAlertDialogHeader,
    HlmAlertDialogTitle,
    HlmAlertDialogDescription,
    HlmAlertDialogFooter,
  ],
  template: `
    <hlm-alert-dialog-header>
      <h3 hlmAlertDialogTitle>{{ data.title }}</h3>
      <p hlmAlertDialogDescription>{{ data.description }}</p>
    </hlm-alert-dialog-header>
    <hlm-alert-dialog-footer class="mt-4 flex justify-end gap-2">
      <button hlmBtn variant="outline" (click)="close(false)">
        {{ data.cancelText || ('common.cancel' | transloco) }}
      </button>
      <button
        hlmBtn
        [variant]="data.variant === 'destructive' ? 'destructive' : 'default'"
        (click)="close(true)"
      >
        {{ data.confirmText || ('common.confirm' | transloco) }}
      </button>
    </hlm-alert-dialog-footer>
  `,
})
export class ConfirmDialogComponent {
  protected readonly data = injectBrnDialogContext<ConfirmDialogData>();
  private readonly dialogRef = inject(BrnDialogRef<boolean>);

  protected close(result: boolean): void {
    this.dialogRef.close(result);
  }
}
