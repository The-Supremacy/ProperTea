import { Routes } from '@angular/router';

export const ORGANIZATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/list/organization-list.component').then(m => m.OrganizationListComponent)
  },
  {
    path: 'setup',
    loadComponent: () => import('./components/organization-setup/organization-setup.component').then(m => m.OrganizationSetupComponent)
  }
];
