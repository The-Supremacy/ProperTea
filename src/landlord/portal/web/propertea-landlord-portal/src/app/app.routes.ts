import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/organizations',
    pathMatch: 'full'
  },
  {
    path: 'organizations',
    loadChildren: () => import('./features/organizations/routes')
      .then(m => m.organizationsRoutes)
  }
  // TODO: Add more feature routes here as they are developed
  // {
  //   path: 'properties',
  //   loadChildren: () => import('./features/properties/routes').then(m => m.propertiesRoutes)
  // },
  // {
  //   path: 'tenants',
  //   loadChildren: () => import('./features/tenants/routes').then(m => m.tenantsRoutes)
  // }
];
