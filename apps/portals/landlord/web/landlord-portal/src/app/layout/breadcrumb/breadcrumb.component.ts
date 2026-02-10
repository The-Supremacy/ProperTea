import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { Router, NavigationEnd, ActivatedRoute, RouterLink } from '@angular/router';
import { filter, map } from 'rxjs';
import { TranslocoPipe } from '@jsverse/transloco';

export interface Breadcrumb {
  label: string;
  url: string;
}

@Component({
  selector: 'app-breadcrumb',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslocoPipe],
  template: `
    <div class="border-b bg-muted/40 px-4 py-2">
      <nav aria-label="breadcrumb">
        <ol class="flex items-center gap-2 text-sm">
          <!-- Home -->
          <li>
            <a
              routerLink="/"
              class="text-muted-foreground hover:text-foreground transition-colors font-medium underline decoration-transparent hover:decoration-current underline-offset-2">
              {{ 'nav.home' | transloco }}
            </a>
          </li>

          <!-- Dynamic breadcrumbs -->
          @for (crumb of breadcrumbs(); track crumb.url; let isLast = $last) {
            <li class="text-muted-foreground">/</li>
            <li>
              @if (isLast) {
                <span class="text-foreground font-semibold">{{ crumb.label }}</span>
              } @else {
                <a
                  [routerLink]="crumb.url"
                  class="text-muted-foreground hover:text-foreground transition-colors font-medium underline decoration-transparent hover:decoration-current underline-offset-2">
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
    if (!route || !route.snapshot) {
      return breadcrumbs;
    }

    const routeData = route.snapshot.data;
    const routePath = route.snapshot.url.map(segment => segment.path).join('/');

    // Build the URL incrementally, even if this route segment has no path
    const nextUrl = routePath ? `${url}/${routePath}` : url;

    // Only add breadcrumb if there's a label and a valid URL
    const label = routeData['breadcrumb'] || routeData['title'];
    if (label) {
      // Use the accumulated URL for this breadcrumb
      const breadcrumbUrl = nextUrl || url;
      if (breadcrumbUrl) {
        breadcrumbs = [...breadcrumbs, { label, url: breadcrumbUrl }];
      }
    }

    if (route.firstChild) {
      return this.buildBreadcrumbs(route.firstChild, nextUrl, breadcrumbs);
    }

    return breadcrumbs;
  }
}
