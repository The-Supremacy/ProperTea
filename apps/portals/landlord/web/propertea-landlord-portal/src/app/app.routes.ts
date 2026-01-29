import { Routes } from '@angular/router';
import { unauthenticatedOnlyGuard } from './auth/guards/unauthenticated-only.guard';
import { authenticatedGuard } from './auth/guards/authenticated.guard';
import { DashboardComponent } from './features/dashboard/pages/dashboard.component';

export const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('./pages/public.routes').then(m => m.PUBLIC_ROUTES),
    canActivate: [unauthenticatedOnlyGuard]
  },
  {
    path: 'organizations/register',
    loadChildren: () => import('./features/organizations/organizations.routes').then(m => m.ORGANIZATION_ROUTES),
    canActivate: [unauthenticatedOnlyGuard]
  },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authenticatedGuard]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
