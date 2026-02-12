import { Routes } from '@angular/router';

export const propertiesRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./list-view/properties-list.component').then((m) => m.PropertiesListComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./details/property-details.component').then((m) => m.PropertyDetailsComponent),
    data: { breadcrumb: 'Property Details' },
  },
];
