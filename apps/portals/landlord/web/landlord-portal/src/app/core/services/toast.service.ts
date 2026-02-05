import { inject, Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

export interface ToastOptions {
  duration?: number;
  action?: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private snackBar = inject(MatSnackBar);

  show(message: string, options: ToastOptions = {}) {
    const config: MatSnackBarConfig = {
      duration: options.duration ?? 3000,
      horizontalPosition: 'end',
      verticalPosition: 'bottom',
    };
    this.snackBar.open(message, options.action, config);
  }

  success(message: string, options: ToastOptions = {}) {
    this.show(`✓ ${message}`, options);
  }

  error(message: string, options: ToastOptions = {}) {
    this.show(`✗ ${message}`, { ...options, duration: 5000 });
  }

  info(message: string, options: ToastOptions = {}) {
    this.show(`ℹ ${message}`, options);
  }

  warning(message: string, options: ToastOptions = {}) {
    this.show(`⚠ ${message}`, options);
  }
}
