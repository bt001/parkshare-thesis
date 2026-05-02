import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { environment } from '../../environments/environment';
import { AuthTokenStorageService } from '../services/auth-token-storage.service';

const anonymousAuthPaths = ['api/auth/login', 'api/auth/refresh'];

export const AuthTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenStorage = inject(AuthTokenStorageService);
  const accessToken = tokenStorage.getAccessToken();

  if (!accessToken || !isParkShareApiRequest(req.url) || isAnonymousAuthRequest(req.url)) {
    return next(req);
  }

  return next(req.clone({
    setHeaders: {
      Authorization: `Bearer ${accessToken}`
    }
  }));
};

function isParkShareApiRequest(url: string): boolean {
  return !/^https?:\/\//i.test(url) || url.startsWith(environment.apiUrl);
}

function isAnonymousAuthRequest(url: string): boolean {
  const normalizedUrl = url.replace(environment.apiUrl, '').replace(/^\/+/, '').toLowerCase();
  return anonymousAuthPaths.includes(normalizedUrl);
}
