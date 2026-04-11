import { TestBed } from '@angular/core/testing';
import { HttpHandlerFn, HttpInterceptorFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';

import { BaseUrlInterceptor } from './base-url.interceptor';
import { environment } from '../../environments/environment';

describe('baseUrlInterceptor', () => {
  const interceptor: HttpInterceptorFn = (req, next) => 
    TestBed.runInInjectionContext(() => BaseUrlInterceptor(req, next));

  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('should be created', () => {
    expect(interceptor).toBeTruthy();
  });

  it('should prepend the API base URL to relative requests', (done) => {
    const request = new HttpRequest('GET', 'api/test/ping');
    const next: HttpHandlerFn = (req) => {
      expect(req.url).toBe(`${environment.apiUrl}api/test/ping`);
      done();
      return of(new HttpResponse({ status: 200 }));
    };

    interceptor(request, next);
  });
});
