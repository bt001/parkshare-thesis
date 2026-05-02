import { Routes } from '@angular/router';

import { authGuard, authLandingGuard } from './auth/auth.guards';
import { AuthRedirectComponent } from './auth/auth-redirect.component';
import { LoginComponent } from './auth/login/login.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { AppShellComponent } from './layout/app-shell.component';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    component: AuthRedirectComponent,
    canActivate: [authLandingGuard]
  },
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent }
    ]
  },
  {
    path: '**',
    component: AuthRedirectComponent,
    canActivate: [authLandingGuard]
  }
];
