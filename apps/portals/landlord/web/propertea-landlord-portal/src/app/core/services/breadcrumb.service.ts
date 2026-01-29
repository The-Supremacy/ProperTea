import { Injectable, signal, computed } from '@angular/core';
import { MenuItem } from 'primeng/api';

@Injectable({
  providedIn: 'root'
})
export class BreadcrumbService {
  private readonly breadcrumbs = signal<MenuItem[]>([]);

  set(items: MenuItem[]): void {
    this.breadcrumbs.set(items);
  }

  clear(): void {
    this.breadcrumbs.set([]);
  }

  getBreadcrumbs = computed(() => this.breadcrumbs());

  hasItems = computed(() => this.breadcrumbs().length > 0);
}
