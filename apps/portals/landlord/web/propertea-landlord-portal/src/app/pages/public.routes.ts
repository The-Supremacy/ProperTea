import { Routes } from '@angular/router';

export const PUBLIC_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./landing/landing-page.component').then(
        (m) => m.LandingPageComponent
      ),
  },
  {
    path: 'documentation',
    loadComponent: () =>
      import('./documentation/documentation.component').then(
        (m) => m.DocumentationComponent
      ),
  },
];
