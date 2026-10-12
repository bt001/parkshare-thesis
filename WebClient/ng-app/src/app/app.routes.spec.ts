import { RenterBookingsComponent } from './bookings/renter-bookings.component';
import { authGuard } from './auth/auth.guards';
import { RenderMode } from '@angular/ssr';
import { AppShellComponent } from './layout/app-shell.component';
import { ListingDetailComponent } from './listings/listing-detail.component';
import { ListingListComponent } from './listings/listing-list.component';
import { OwnerAvailabilityComponent } from './owner/owner-availability.component';
import { OwnerBookingsComponent } from './owner/owner-bookings.component';
import { OwnerListingFormComponent } from './owner/owner-listing-form.component';
import { OwnerListingsComponent } from './owner/owner-listings.component';
import { routes } from './app.routes';
import { serverRoutes } from './app.routes.server';

describe('application routes', () => {
  it('keeps listing browse and detail routes inside the protected shell', () => {
    const shellRoute = routes.find((route) => route.component === AppShellComponent);

    expect(shellRoute?.canActivate).toContain(authGuard);
    expect(shellRoute?.canActivateChild).toContain(authGuard);
    expect(shellRoute?.children).toContain(jasmine.objectContaining({
      path: 'listings',
      component: ListingListComponent
    }));
    expect(shellRoute?.children).toContain(jasmine.objectContaining({
      path: 'listings/:id',
      component: ListingDetailComponent
    }));
  });

  it('keeps the complete owner workspace inside the authenticated shell', async () => {
    const children = routes.find((route) => route.component === AppShellComponent)?.children;
    expect(children).toContain(jasmine.objectContaining({ path: 'owner/listings', component: OwnerListingsComponent }));
    for (const path of ['owner/listings/new', 'owner/listings/:id/edit']) {
      expect(await children?.find(route => route.path === path)?.loadComponent?.()).toBe(OwnerListingFormComponent);
    }
    expect(children).toContain(jasmine.objectContaining({ path: 'owner/listings/:id/availability', component: OwnerAvailabilityComponent }));
    expect(children).toContain(jasmine.objectContaining({ path: 'owner/bookings', component: OwnerBookingsComponent }));
  });

  it('uses client rendering for every protected route and the landing route', () => {
    const children = routes.find(route => route.component === AppShellComponent)!.children!;
    for (const path of ['', 'session-recovery', ...children.map(route => route.path!)]) {
      const renderRoute = serverRoutes.find(route => route.path === path || route.path === '**');
      expect(renderRoute?.renderMode).withContext(path).toBe(RenderMode.Client);
    }
    expect(serverRoutes.filter(route => route.renderMode === RenderMode.Prerender).map(route => route.path))
      .toEqual(['login', 'register']);
  });

  it('keeps renter history and notifications lazy inside the protected shell', async () => {
    const shell = routes.find(route => route.component === AppShellComponent);
    expect(shell?.canActivate).toContain(authGuard);
    const history = shell?.children?.find(route => route.path === 'bookings');
    expect(await history?.loadComponent?.()).toBe(RenterBookingsComponent);
    const inbox = shell?.children?.find(route => route.path === 'notifications');
    expect(inbox?.loadComponent).toBeDefined();
  });
});
