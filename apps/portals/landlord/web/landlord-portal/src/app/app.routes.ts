import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { organizationsRoutes } from './features/organizations/organizations.routes';
import { companiesRoutes } from './features/companies/companies.routes';
import { propertiesRoutes } from './features/properties/routes';
// Units feature temporarily removed from navigation
// import { unitsRoutes } from './features/units/routes';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
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
      {
        path: 'companies',
        data: { breadcrumb: 'Companies' },
        children: companiesRoutes,
      },
      {
        path: 'properties',
        data: { breadcrumb: 'Properties' },
        children: propertiesRoutes,
      },
      // Units feature temporarily removed from navigation
      // {
      //   path: 'units',
      //   data: { breadcrumb: 'Units' },
      //   children: unitsRoutes,
      // },
    ],
  },
  { path: '**', redirectTo: '' },
];
