import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { interval, startWith, switchMap, catchError, of, Subscription } from 'rxjs';

export interface HealthStatus {
  isHealthy: boolean;
  lastChecked: Date;
}

@Injectable({
  providedIn: 'root'
})
export class HealthService {
  private http = inject(HttpClient);

  private subscription: Subscription | null = null;
  private monitoring = false;

  private healthStatus = signal<HealthStatus>({
    isHealthy: true,
    lastChecked: new Date()
  });

  readonly status = this.healthStatus.asReadonly();

  startMonitoring(intervalMs = 30000): void {
    if (this.monitoring)
      return;

    this.monitoring = true;
    this.subscription = interval(intervalMs)
      .pipe(
        startWith(0),
        switchMap(() =>
          this.http.get('/health', { responseType: 'text' }).pipe(
            catchError(() => of(null))
          )
        )
      )
      .subscribe(response => {
        this.healthStatus.set({
          isHealthy: response !== null,
          lastChecked: new Date()
        });
      });
  }

  stopMonitoring(): void {
    this.subscription?.unsubscribe();
    this.subscription = null;
    this.monitoring = false;
  }
}
