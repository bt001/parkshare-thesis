import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';

import { AuthService } from '../api/auth/auth.service';
import { authGuard, authLandingGuard } from './auth.guards';

describe('auth guards', () => {
  const authService = jasmine.createSpyObj<AuthService>('AuthService', ['hasAuthenticatedSession']);
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService }
      ]
    });

    router = TestBed.inject(Router);
  });

  it('redirects anonymous users to login from protected routes', () => {
    authService.hasAuthenticatedSession.and.returnValue(false);

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/dashboard' } as never)
    );

    expect(result instanceof UrlTree).toBeTrue();
    expect(router.serializeUrl(result as UrlTree)).toBe('/login?returnUrl=%2Fdashboard');
  });

  it('allows authenticated users through protected routes', () => {
    authService.hasAuthenticatedSession.and.returnValue(true);

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/dashboard' } as never)
    );

    expect(result).toBeTrue();
  });

  it('redirects anonymous root visits to login', () => {
    authService.hasAuthenticatedSession.and.returnValue(false);

    const result = TestBed.runInInjectionContext(() => authLandingGuard({} as never, {} as never));

    expect(router.serializeUrl(result as UrlTree)).toBe('/login');
  });

  it('redirects authenticated root visits to dashboard', () => {
    authService.hasAuthenticatedSession.and.returnValue(true);

    const result = TestBed.runInInjectionContext(() => authLandingGuard({} as never, {} as never));

    expect(router.serializeUrl(result as UrlTree)).toBe('/dashboard');
  });
});
