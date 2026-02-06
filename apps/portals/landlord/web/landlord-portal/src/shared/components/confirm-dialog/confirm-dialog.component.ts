import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
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
  imports: [ButtonDirective, TranslocoPipe],
  template: `
    <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/50" (click)="cancel.emit()">
      <div class="max-w-md rounded-lg border bg-background p-6 shadow-lg" (click)="$event.stopPropagation()">
        <h2 class="text-lg font-semibold">{{ data().title }}</h2>
        <p class="mt-2 text-sm text-muted-foreground">
          {{ data().description }}
        </p>
        <div class="mt-6 flex gap-2 justify-end">
          <button
            appBtn
            variant="outline"
            (click)="cancel.emit()">
            {{ data().cancelText || ('common.cancel' | transloco) }}
          </button>
          <button
            appBtn
            [variant]="data().variant === 'destructive' ? 'destructive' : 'default'"
            (click)="confirm.emit()">
            {{ data().confirmText || ('common.confirm' | transloco) }}
          </button>
        </div>
      </div>
    </div>
  `
})
export class ConfirmDialogComponent {
  data = input.required<ConfirmDialogData>();
  confirm = output<void>();
  cancel = output<void>();
}
