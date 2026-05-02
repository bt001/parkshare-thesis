import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../api/auth/auth.service';

export const authGuard: CanActivateFn = (_, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.hasAuthenticatedSession()
    ? true
    : router.createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url && state.url !== '/' ? state.url : '/dashboard' }
    });
};

export const authLandingGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return router.createUrlTree([authService.hasAuthenticatedSession() ? '/dashboard' : '/login']);
};
