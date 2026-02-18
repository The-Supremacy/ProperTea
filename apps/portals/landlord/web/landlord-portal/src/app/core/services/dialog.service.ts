import { inject, Injectable } from '@angular/core';
import { map, take } from 'rxjs';
import { Observable } from 'rxjs';
import { HlmDialogService } from '@spartan-ng/helm/dialog';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/components/confirm-dialog';

@Injectable({ providedIn: 'root' })
export class DialogService {
  private readonly dialogService = inject(HlmDialogService);

  confirm(data: ConfirmDialogData): Observable<boolean> {
    const ref = this.dialogService.open(ConfirmDialogComponent, {
      context: data,
    });

    return ref.closed$.pipe(
      take(1),
      map((result) => result === true),
    );
  }
}
