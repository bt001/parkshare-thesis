import { Routes } from '@angular/router';

import { authGuard, authLandingGuard } from './auth/auth.guards';
import { AuthRedirectComponent } from './auth/auth-redirect.component';



import { DashboardComponent } from './dashboard/dashboard.component';
import { AppShellComponent } from './layout/app-shell.component';
import { ListingDetailComponent } from './listings/listing-detail.component';
import { ListingListComponent } from './listings/listing-list.component';
import { OwnerAvailabilityComponent } from './owner/owner-availability.component';
import { OwnerBookingsComponent } from './owner/owner-bookings.component';
import { OwnerListingFormComponent } from './owner/owner-listing-form.component';
import { OwnerListingsComponent } from './owner/owner-listings.component';

export const routes: Routes = [
  { path: 'session-recovery', loadComponent: () => import('./auth/session-recovery.component').then(module => module.SessionRecoveryComponent) },
  {
    path: '',
    pathMatch: 'full',
    component: AuthRedirectComponent,
    canActivate: [authLandingGuard]
  },
  {
    path: 'login',
    loadComponent: () => import('./auth/login/login.component').then(module => module.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./auth/register/register.component').then(module => module.RegisterComponent)
  },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: 'bookings/:id/chat', loadComponent: () => import('./bookings/booking-chat.component').then(module => module.BookingChatComponent) },
      { path: 'notifications', loadComponent: () => import('./notifications/notifications.component').then(module => module.NotificationsComponent) },
      { path: 'bookings', loadComponent: () => import('./bookings/renter-bookings.component').then(module => module.RenterBookingsComponent) },
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
