import { Routes } from '@angular/router';

export const organizationsRoutes: Routes = [
  {
    path: 'register',
    loadComponent: () =>
      import('./pages/register/register-organization.page').then((m) => m.RegisterOrganizationPage),
  },
];
