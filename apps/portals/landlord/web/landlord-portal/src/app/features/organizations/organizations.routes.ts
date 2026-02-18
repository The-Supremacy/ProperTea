import { Routes } from '@angular/router';

export const organizationsRoutes: Routes = [
  {
    path: 'details',
    loadComponent: () =>
      import('./details/organization-details.component').then((m) => m.OrganizationDetailsComponent),
    data: { breadcrumb: 'organizations.detailsTitle' },
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./pages/register/register-organization.page').then((m) => m.RegisterOrganizationPage),
  },
];
