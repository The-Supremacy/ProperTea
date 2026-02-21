import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { Router, NavigationEnd, ActivatedRoute } from '@angular/router';
import { filter, map } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe } from '@jsverse/transloco';
import { HlmBreadCrumbImports } from '@spartan-ng/helm/breadcrumb';

export interface Breadcrumb {
  label: string;
  url: string;
}

@Component({
  selector: 'app-breadcrumb',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmBreadCrumbImports, TranslocoPipe],
  template: `
    <div class="border-b bg-muted/40 px-4 py-2">
      <nav hlmBreadcrumb>
        <ol hlmBreadcrumbList>
          <li hlmBreadcrumbItem>
            <a hlmBreadcrumbLink link="/">{{ 'nav.home' | transloco }}</a>
          </li>

          @for (crumb of breadcrumbs(); track $index; let isLast = $last) {
            <li hlmBreadcrumbSeparator></li>
            <li hlmBreadcrumbItem>
              @if (isLast) {
                <span hlmBreadcrumbPage>{{ crumb.label | transloco }}</span>
              } @else {
                <a hlmBreadcrumbLink [link]="crumb.url">{{ crumb.label | transloco }}</a>
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
        map(() => this.buildBreadcrumbs(this.activatedRoute.root)),
        takeUntilDestroyed(),
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
