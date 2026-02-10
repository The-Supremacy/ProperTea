import { Routes } from '@angular/router';
import { CompaniesListComponent } from './list-view/companies-list.component';
import { CompanyDetailsComponent } from './details/company-details.component';

export const companiesRoutes: Routes = [
  {
    path: '',
    component: CompaniesListComponent,
  },
  {
    path: ':id',
    component: CompanyDetailsComponent,
    data: { breadcrumb: 'Company Details' },
  },
];
