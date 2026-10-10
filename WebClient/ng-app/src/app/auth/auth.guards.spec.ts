import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { firstValueFrom, Observable, of, Subject, throwError } from 'rxjs';
import { AuthService, SessionChangedError, SessionOutcome } from '../api/auth/auth.service';
import { authGuard, authLandingGuard } from './auth.guards';

describe('asynchronous auth guards', () => {
  const auth = jasmine.createSpyObj<AuthService>('AuthService', ['ensureSession']);
  let router: Router;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), { provide: AuthService, useValue: auth }] });
    router = TestBed.inject(Router);
  });
  for (const outcome of ['authenticated', 'signed-out', 'unavailable'] as SessionOutcome[]) {
    it('resolves protected navigation after ' + outcome, async () => {
      auth.ensureSession.and.returnValue(of(outcome));
      const result = await firstValueFrom(TestBed.runInInjectionContext(() =>
        authGuard({} as never, { url: '/bookings?status=0' } as never)) as Observable<boolean | UrlTree>);
      if (outcome === 'authenticated') expect(result).toBeTrue();
      else expect(router.serializeUrl(result as UrlTree)).toBe(
        (outcome === 'unavailable' ? '/session-recovery' : '/login') + '?returnUrl=%2Fbookings%3Fstatus%3D0');
    });

    it('resolves landing navigation after ' + outcome, async () => {
      auth.ensureSession.and.returnValue(of(outcome));
      const result = await firstValueFrom(TestBed.runInInjectionContext(() =>
        authLandingGuard({} as never, {} as never)) as Observable<UrlTree>);
      expect(router.serializeUrl(result)).toBe(outcome === 'authenticated' ? '/dashboard'
        : outcome === 'signed-out' ? '/login' : '/session-recovery?returnUrl=%2Fdashboard');
    });
  }

  it('does not activate while recovery is pending', () => {
    const pending = new Subject<SessionOutcome>();
    auth.ensureSession.and.returnValue(pending);
    const results: unknown[] = [];
    (TestBed.runInInjectionContext(() => authGuard({} as never, { url: '/bookings' } as never)) as Observable<unknown>).subscribe(value => results.push(value));
    expect(results).toEqual([]);
    pending.next('authenticated');
    expect(results).toEqual([true]);
  });

  it('cancels obsolete navigation after a session change', async () => {
    auth.ensureSession.and.returnValue(throwError(() => new SessionChangedError()));
    expect(await firstValueFrom(TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/bookings' } as never)) as Observable<boolean>)).toBeFalse();
  });
});
