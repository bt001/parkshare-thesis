import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { SessionNavigationService } from './session-navigation.service';

/** Waits for recovery before entering any protected page. */
export const authGuard: CanActivateFn = (_, state) => {
  const auth = inject(AuthService);
  const navigation = inject(SessionNavigationService);
  return auth.ensureSession().pipe(
    map(outcome => outcome === 'authenticated' ? true : navigation.destination(outcome, state.url)),
    catchError(() => of(false))
  );
};

/** Resolves the app entry only after browser session recovery settles. */
export const authLandingGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const navigation = inject(SessionNavigationService);
  return auth.ensureSession().pipe(
    map(outcome => outcome === 'authenticated' ? router.createUrlTree(['/dashboard'])
      : outcome === 'signed-out' ? router.createUrlTree(['/login']) : navigation.destination(outcome, '/dashboard')),
    catchError(() => of(false))
  );
};
