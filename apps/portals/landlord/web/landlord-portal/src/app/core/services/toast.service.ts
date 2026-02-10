import { inject, Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';
import { TranslocoService } from '@jsverse/transloco';

export interface ToastOptions {
  duration?: number;
  action?: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private snackBar = inject(MatSnackBar);
  private translocoService = inject(TranslocoService);

  show(messageKey: string, options: ToastOptions = {}) {
    const message = this.translocoService.translate(messageKey);
    const config: MatSnackBarConfig = {
      duration: options.duration ?? 3000,
      horizontalPosition: 'end',
      verticalPosition: 'bottom',
      panelClass: ['toast-panel'],
    };
    this.snackBar.open(message, options.action, config);
  }

  success(messageKey: string, options: ToastOptions = {}) {
    const message = this.translocoService.translate(messageKey);
    this.show(`✓ ${message}`, options);
  }

  error(messageKey: string, options: ToastOptions = {}) {
    const message = this.translocoService.translate(messageKey);
    this.show(`✗ ${message}`, { ...options, duration: 5000 });
  }

  info(messageKey: string, options: ToastOptions = {}) {
    const message = this.translocoService.translate(messageKey);
    this.show(`ℹ ${message}`, options);
  }

  warning(messageKey: string, options: ToastOptions = {}) {
    const message = this.translocoService.translate(messageKey);
    this.show(`⚠ ${message}`, options);
  }
}
