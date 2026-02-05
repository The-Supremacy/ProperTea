import { inject, Injectable, ApplicationRef, createComponent, EnvironmentInjector } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { ConfirmDialogComponent, ConfirmDialogData } from '../components/confirm-dialog/confirm-dialog.component';

@Injectable({ providedIn: 'root' })
export class DialogService {
  private appRef = inject(ApplicationRef);
  private injector = inject(EnvironmentInjector);

  confirm(data: ConfirmDialogData): Observable<boolean> {
    const subject = new Subject<boolean>();

    const componentRef = createComponent(ConfirmDialogComponent, {
      environmentInjector: this.injector,
    });

    componentRef.setInput('data', data);

    componentRef.instance.confirm.subscribe(() => {
      subject.next(true);
      subject.complete();
      this.cleanup(componentRef);
    });

    componentRef.instance.cancel.subscribe(() => {
      subject.next(false);
      subject.complete();
      this.cleanup(componentRef);
    });

    this.appRef.attachView(componentRef.hostView);
    const domElem = (componentRef.hostView as any).rootNodes[0] as HTMLElement;
    document.body.appendChild(domElem);

    return subject.asObservable();
  }

  private cleanup(componentRef: any) {
    this.appRef.detachView(componentRef.hostView);
    componentRef.destroy();
  }
}
