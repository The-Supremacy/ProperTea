import { inject, Injectable } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { toast } from 'ngx-sonner';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly transloco = inject(TranslocoService);

  private t(key: string): string {
    return this.transloco.translate(key);
  }

  show(messageKey: string): void {
    toast(this.t(messageKey));
  }

  success(messageKey: string): void {
    toast.success(this.t(messageKey));
  }

  error(messageKey: string): void {
    toast.error(this.t(messageKey));
  }

  info(messageKey: string): void {
    toast.info(this.t(messageKey));
  }

  warning(messageKey: string): void {
    toast.warning(this.t(messageKey));
  }
}
