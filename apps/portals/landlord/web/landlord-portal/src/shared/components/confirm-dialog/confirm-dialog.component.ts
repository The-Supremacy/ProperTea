import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonDirective } from '../button';

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
  imports: [MatDialogModule, ButtonDirective, TranslocoPipe],
  template: `
    <div class="p-6 text-foreground">
      <h2 class="text-lg font-semibold" mat-dialog-title>{{ data.title }}</h2>
      <div mat-dialog-content class="mt-2">
        <p class="text-sm text-muted-foreground">
          {{ data.description }}
        </p>
      </div>
      <div mat-dialog-actions class="mt-6 flex justify-end gap-2">
        <button appBtn variant="outline" [mat-dialog-close]="false">
          {{ data.cancelText || ('common.cancel' | transloco) }}
        </button>
        <button
          appBtn
          [variant]="data.variant === 'destructive' ? 'destructive' : 'default'"
          [mat-dialog-close]="true"
        >
          {{ data.confirmText || ('common.confirm' | transloco) }}
        </button>
      </div>
    </div>
  `,
})
export class ConfirmDialogComponent {
  data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
  dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);
}
