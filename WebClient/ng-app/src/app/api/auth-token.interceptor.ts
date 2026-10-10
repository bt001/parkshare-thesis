import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { catchError, of, switchMap, tap, throwError } from 'rxjs';
import { ANONYMOUS_API, apiPath, isAuthPath } from './api-request';
import { AuthService, SessionChangedError } from './auth/auth.service';
import { SessionNavigationService } from '../auth/session-navigation.service';

/** Recovers protected API credentials with a single bounded authentication replay. */
export const AuthTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const path = apiPath(req.url);
  if (path === null || isAuthPath(path) || req.context.get(ANONYMOUS_API)) return next(req);
  const injector = inject(Injector);
  const auth = injector.get(AuthService);
  const navigation = injector.get(SessionNavigationService);
  const generation = auth.sessionGeneration;

  return auth.ensureSession().pipe(
    switchMap(outcome => {
      if (outcome !== 'authenticated') {
        navigation.showFailure(outcome);
        return throwError(() => new Error(outcome === 'unavailable' ? 'Session recovery is temporarily unavailable.' : 'Please sign in.'));
      }
      auth.assertIdentity(generation);
      const token = auth.session!.accessToken;
      const revision = auth.sessionRevision;
      const send = (accessToken: string) => next(req.clone({ setHeaders: { Authorization: 'Bearer ' + accessToken } })).pipe(
        tap(() => auth.assertIdentity(generation))
      );
      return send(token).pipe(catchError(error => {
        auth.assertIdentity(generation);
        if (!(error instanceof HttpErrorResponse) || error.status !== 401
          || (error.error !== null && error.error !== undefined && error.error !== '')) return throwError(() => error);
        const refreshed = auth.sessionRevision !== revision && auth.hasAuthenticatedSession()
          ? of(auth.session!) : auth.recover(true);
        return refreshed.pipe(
          catchError(refreshError => {
            if (!(refreshError instanceof SessionChangedError)) navigation.showFailure(auth.session ? 'unavailable' : 'signed-out');
            return throwError(() => refreshError);
          }),
          switchMap(session => {
            auth.assertIdentity(generation);
            return send(session.accessToken);
          })
        );
      }));
    })
  );
};
