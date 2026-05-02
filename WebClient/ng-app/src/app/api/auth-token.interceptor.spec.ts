import { TestBed } from '@angular/core/testing';
import { HttpHandlerFn, HttpInterceptorFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';

import { AuthTokenStorageService } from '../services/auth-token-storage.service';
import { AuthTokenInterceptor } from './auth-token.interceptor';

describe('AuthTokenInterceptor', () => {
  const tokenStorage = jasmine.createSpyObj<AuthTokenStorageService>('AuthTokenStorageService', ['getAccessToken']);
  const interceptor: HttpInterceptorFn = (req, next) =>
    TestBed.runInInjectionContext(() => AuthTokenInterceptor(req, next));

  beforeEach(() => {
    tokenStorage.getAccessToken.and.returnValue('access-token');

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthTokenStorageService, useValue: tokenStorage }
      ]
    });
  });

  it('adds bearer authorization to API requests', (done) => {
    const request = new HttpRequest('GET', 'api/listing/owner');
    const next: HttpHandlerFn = (req) => {
      expect(req.headers.get('Authorization')).toBe('Bearer access-token');
      done();
      return of(new HttpResponse({ status: 200 }));
    };

    interceptor(request, next);
  });

  it('does not add bearer authorization to login requests', (done) => {
    const request = new HttpRequest('POST', 'api/auth/login', {});
    const next: HttpHandlerFn = (req) => {
      expect(req.headers.has('Authorization')).toBeFalse();
      done();
      return of(new HttpResponse({ status: 200 }));
    };

    interceptor(request, next);
  });
});
