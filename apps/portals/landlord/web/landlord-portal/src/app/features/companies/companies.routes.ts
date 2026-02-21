import { Routes } from '@angular/router';

export const companiesRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./list-view/companies-list.component').then((m) => m.CompaniesListComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./details/company-details.component').then((m) => m.CompanyDetailsComponent),
    data: { breadcrumb: 'companies.detailsTitle' },
  },
];
