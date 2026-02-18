import { Routes } from '@angular/router';

export const buildingsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./list-view/buildings-list.component').then((m) => m.BuildingsListComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./details/building-details.component').then((m) => m.BuildingDetailsComponent),
    data: { breadcrumb: 'buildings.details.breadcrumb' },
  },
];
