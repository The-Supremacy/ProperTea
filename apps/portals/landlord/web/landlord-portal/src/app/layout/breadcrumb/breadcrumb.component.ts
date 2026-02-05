import { Component, inject, signal, effect } from '@angular/core';
import { Router, NavigationEnd, ActivatedRoute, RouterLink } from '@angular/router';
import { filter, map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';

export interface Breadcrumb {
  label: string;
  url: string;
}

@Component({
  selector: 'app-breadcrumb',
  imports: [RouterLink],
  template: `
    <div class="border-b bg-muted/40 px-4 py-2">
      <nav aria-label="breadcrumb">
        <ol class="flex items-center gap-2 text-sm">
          <!-- Home -->
          <li>
            <a routerLink="/" class="hover:text-foreground transition-colors">Home</a>
          </li>

          <!-- Dynamic breadcrumbs -->
          @for (crumb of breadcrumbs(); track crumb.url; let isLast = $last) {
            <li class="text-muted-foreground">/</li>
            <li>
              @if (isLast) {
                <span class="text-foreground font-medium">{{ crumb.label }}</span>
              } @else {
                <a [routerLink]="crumb.url" class="hover:text-foreground transition-colors">
                  {{ crumb.label }}
                </a>
              }
            </li>
          }
        </ol>
      </nav>
    </div>
  `
})
export class BreadcrumbComponent {
  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);

  protected breadcrumbs = signal<Breadcrumb[]>([]);

  constructor() {
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        map(() => this.buildBreadcrumbs(this.activatedRoute.root))
      )
      .subscribe(breadcrumbs => this.breadcrumbs.set(breadcrumbs));

    this.breadcrumbs.set(this.buildBreadcrumbs(this.activatedRoute.root));
  }

  private buildBreadcrumbs(
    route: ActivatedRoute,
    url: string = '',
    breadcrumbs: Breadcrumb[] = []
  ): Breadcrumb[] {
    const routeData = route.snapshot.data;
    const routePath = route.snapshot.url.map(segment => segment.path).join('/');

    const nextUrl = routePath ? `${url}/${routePath}` : url;

    const label = routeData['breadcrumb'] || routeData['title'];
    if (label && nextUrl) {
      breadcrumbs = [...breadcrumbs, { label, url: nextUrl }];
    }

    if (route.firstChild) {
      return this.buildBreadcrumbs(route.firstChild, nextUrl, breadcrumbs);
    }

    return breadcrumbs;
  }
}
