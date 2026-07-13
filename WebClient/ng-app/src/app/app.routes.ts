import { Routes } from '@angular/router';

import { authGuard, authLandingGuard } from './auth/auth.guards';
import { AuthRedirectComponent } from './auth/auth-redirect.component';
import { LoginComponent } from './auth/login/login.component';
import { RegisterComponent } from './auth/register/register.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { AppShellComponent } from './layout/app-shell.component';
import { ListingDetailComponent } from './listings/listing-detail.component';
import { ListingListComponent } from './listings/listing-list.component';
import { OwnerAvailabilityComponent } from './owner/owner-availability.component';
import { OwnerBookingsComponent } from './owner/owner-bookings.component';
import { OwnerListingFormComponent } from './owner/owner-listing-form.component';
import { OwnerListingsComponent } from './owner/owner-listings.component';

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
    path: 'register',
    component: RegisterComponent
  },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent },
      { path: 'listings', component: ListingListComponent },
      { path: 'listings/:id', component: ListingDetailComponent },
      { path: 'owner/listings', component: OwnerListingsComponent },
      { path: 'owner/listings/new', component: OwnerListingFormComponent },
      { path: 'owner/listings/:id/edit', component: OwnerListingFormComponent },
      { path: 'owner/listings/:id/availability', component: OwnerAvailabilityComponent },
      { path: 'owner/bookings', component: OwnerBookingsComponent }
    ]
  },
  {
    path: '**',
    component: AuthRedirectComponent,
    canActivate: [authLandingGuard]
  }
];
