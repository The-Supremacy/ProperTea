import { Routes } from '@angular/router';

export const unitsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./list-view/units-list.component').then((m) => m.UnitsListComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./details/unit-details.component').then((m) => m.UnitDetailsComponent),
    data: { breadcrumb: 'units.details.breadcrumb' },
  },
];
