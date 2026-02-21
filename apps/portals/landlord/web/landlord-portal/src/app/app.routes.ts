import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import {
  organizationPublicRoutes,
  organizationProtectedRoutes,
} from './features/organizations/organizations.routes';
import { companiesRoutes } from './features/companies/companies.routes';
import { propertiesRoutes } from './features/properties/routes';
import { buildingsRoutes } from './features/buildings/routes';
import { unitsRoutes } from './features/units/routes';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./features/landing/landing.page').then((m) => m.LandingPage),
  },
  {
    path: 'organizations',
    children: organizationPublicRoutes,
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
        data: { breadcrumb: 'nav.dashboard' },
        loadComponent: () =>
          import('./features/dashboard/dashboard-home.page').then((m) => m.DashboardHomePage),
      },
      {
        path: 'organizations',
        data: { breadcrumb: 'nav.organization' },
        children: organizationProtectedRoutes,
      },
      {
        path: 'companies',
        data: { breadcrumb: 'nav.companies' },
        children: companiesRoutes,
      },
      {
        path: 'properties',
        data: { breadcrumb: 'nav.properties' },
        children: propertiesRoutes,
      },
      {
        path: 'buildings',
        data: { breadcrumb: 'nav.buildings' },
        children: buildingsRoutes,
      },
      {
        path: 'units',
        data: { breadcrumb: 'nav.units' },
        children: unitsRoutes,
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
