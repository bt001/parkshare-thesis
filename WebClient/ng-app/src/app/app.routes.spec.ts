import { authGuard } from './auth/auth.guards';
import { RenderMode } from '@angular/ssr';
import { AppShellComponent } from './layout/app-shell.component';
import { ListingDetailComponent } from './listings/listing-detail.component';
import { ListingListComponent } from './listings/listing-list.component';
import { routes } from './app.routes';
import { serverRoutes } from './app.routes.server';

describe('application routes', () => {
  it('keeps listing browse and detail routes inside the protected shell', () => {
    const shellRoute = routes.find((route) => route.component === AppShellComponent);

    expect(shellRoute?.canActivate).toContain(authGuard);
    expect(shellRoute?.children).toContain(jasmine.objectContaining({
      path: 'listings',
      component: ListingListComponent
    }));
    expect(shellRoute?.children).toContain(jasmine.objectContaining({
      path: 'listings/:id',
      component: ListingDetailComponent
    }));
  });

  it('uses client rendering for the authenticated parameterized detail route', () => {
    expect(serverRoutes).toContain(jasmine.objectContaining({
      path: 'listings/:id',
      renderMode: RenderMode.Client
    }));
  });
});
