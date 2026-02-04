import { Injectable, inject } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ResponsiveService {
  private breakpointObserver = inject(BreakpointObserver);

  isMobile = toSignal(
    this.breakpointObserver
      .observe([Breakpoints.HandsetPortrait, Breakpoints.HandsetLandscape])
      .pipe(map(result => result.matches)),
    { initialValue: false }
  );

  isTablet = toSignal(
    this.breakpointObserver
      .observe([Breakpoints.TabletPortrait, Breakpoints.TabletLandscape])
      .pipe(map(result => result.matches)),
    { initialValue: false }
  );

  isDesktop = toSignal(
    this.breakpointObserver
      .observe([Breakpoints.Web, Breakpoints.WebLandscape, Breakpoints.WebPortrait])
      .pipe(map(result => result.matches)),
    { initialValue: false }
  );

  isHandheld = toSignal(
    this.breakpointObserver
      .observe([Breakpoints.Handset])
      .pipe(map(result => result.matches)),
    { initialValue: false }
  );
}
