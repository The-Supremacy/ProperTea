import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { organizationsRoutes } from './features/organizations/organizations.routes';

export const routes: Routes = [
  // Public routes
  {
    path: '',
    loadComponent: () => import('./features/landing/landing.page').then((m) => m.LandingPage),
  },
  {
    path: 'organizations',
    children: organizationsRoutes,
  },
  {
    path: 'docs',
    loadComponent: () => import('./features/docs/docs.page').then((m) => m.DocsPage),
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        data: { breadcrumb: 'Dashboard' },
        loadComponent: () =>
          import('./features/dashboard/dashboard-home.page').then((m) => m.DashboardHomePage),
      },
    ],
  },
];
