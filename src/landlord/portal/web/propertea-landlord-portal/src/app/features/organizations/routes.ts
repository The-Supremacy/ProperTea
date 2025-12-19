import { Routes } from '@angular/router';

export const organizationsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/organizations-list/organizations-list.component')
      .then(m => m.OrganizationsListComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./pages/organization-detail/organization-detail.component')
      .then(m => m.OrganizationDetailComponent)
  }
  // TODO: Add routes for create and edit when those components are created
  // {
  //   path: 'new',
  //   loadComponent: () => import('./pages/organization-form/organization-form.component')
  //     .then(m => m.OrganizationFormComponent)
  // },
  // {
  //   path: ':id/edit',
  //   loadComponent: () => import('./pages/organization-form/organization-form.component')
  //     .then(m => m.OrganizationFormComponent)
  // }
];
