import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService, SessionChangedError } from './auth/auth.service';
import { testSession } from './auth/auth-test.helpers';
import { sessionStorageKey } from '../services/auth-token-storage.service';
import { SessionNavigationService } from '../auth/session-navigation.service';
import { ANONYMOUS_API } from './api-request';
import { AuthTokenInterceptor } from './auth-token.interceptor';
import { BaseUrlInterceptor } from './base-url.interceptor';
import { environment } from '../../environments/environment';

describe('authentication interceptor chain', () => {
  let client: HttpClient;
  let http: HttpTestingController;
  let auth: AuthService;
  const url = (path: string) => environment.apiUrl + path;
  const navigation = jasmine.createSpyObj<SessionNavigationService>('navigation', ['showFailure']);
  beforeEach(() => {
    navigation.showFailure.calls.reset();
    localStorage.setItem(sessionStorageKey, JSON.stringify(testSession()));
    TestBed.configureTestingModule({ providers: [
      provideRouter([]), provideHttpClient(withInterceptors([BaseUrlInterceptor, AuthTokenInterceptor])),
      provideHttpClientTesting(), { provide: SessionNavigationService, useValue: navigation }
    ] });
    client = TestBed.inject(HttpClient); http = TestBed.inject(HttpTestingController); auth = TestBed.inject(AuthService);
  });
  afterEach(() => { http.verify(); localStorage.removeItem(sessionStorageKey); });

  it('attaches access tokens to configured API requests', () => {
    client.get('api/listing/owner').subscribe();
    const req = http.expectOne(url('api/listing/owner'));
    expect(req.request.headers.get('Authorization')).toBe('Bearer access');
    req.flush({});
  });

  for (const path of ['api/auth/login?x=1', 'api/auth/register/', 'api/auth/refresh', 'http://external.test/api/a', 'https://external.test/api/a', '//external.test/api/a', '/assets/a.json', environment.apiUrl + 'api-other/a', 'https://localhost:6002.evil.test/api/a']) {
    it('excludes auth/external/resource request ' + path, () => {
      client.get(path).subscribe();
      const expected = path.startsWith('api/') ? url(path) : path;
      const request = http.expectOne(expected);
      expect(request.request.headers.has('Authorization')).toBeFalse();
      request.flush({});
      http.expectNone(url('api/auth/refresh'));
    });
  }

  it('keeps anonymous reads usable without credentials', () => {
    auth.logout();
    client.get('api/listing/search', { context: new HttpContext().set(ANONYMOUS_API, true) }).subscribe();
    const request = http.expectOne(url('api/listing/search'));
    expect(request.request.headers.has('Authorization')).toBeFalse();
    request.flush({});
  });

  it('refreshes before concurrent expired-token mutations and sends each only once', () => {
    auth.session!.accessTokenExpiresAt = '2000-01-01';
    localStorage.setItem(sessionStorageKey, JSON.stringify(auth.session));
    client.post('api/booking/bookingrequest', { listingId: 1 }).subscribe();
    client.get('api/booking/renter').subscribe();
    http.expectNone(url('api/booking/bookingrequest'));
    http.expectOne(url('api/auth/refresh')).flush(testSession({ accessToken: 'renewed' }));
    const mutation = http.expectOne(url('api/booking/bookingrequest'));
    expect(mutation.request.headers.get('Authorization')).toBe('Bearer renewed');
    mutation.flush({});
    http.expectOne(url('api/booking/renter')).flush({});
  });

  it('coalesces concurrent and delayed 401s even when refreshed JWT strings are identical', () => {
    for (const path of ['one', 'two', 'late']) client.get('api/' + path).subscribe();
    const one = http.expectOne(url('api/one'));
    const two = http.expectOne(url('api/two'));
    const late = http.expectOne(url('api/late'));
    one.flush(null, { status: 401, statusText: 'Unauthorized' });
    two.flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne(url('api/auth/refresh')).flush(testSession());
    late.flush(null, { status: 401, statusText: 'Unauthorized' });
    for (const path of ['one', 'two', 'late']) {
      const retry = http.expectOne(url('api/' + path));
      expect(retry.request.headers.get('Authorization')).toBe('Bearer access');
      retry.flush({});
    }
  });

  it('surfaces a second 401 without logout or a second refresh', () => {
    let status = 0;
    client.get('api/owner').subscribe({ error: error => status = error.status });
    http.expectOne(url('api/owner')).flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne(url('api/auth/refresh')).flush(testSession({ accessToken: 'renewed' }));
    http.expectOne(url('api/owner')).flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(status).toBe(401);
    expect(auth.session).not.toBeNull();
    expect(navigation.showFailure).not.toHaveBeenCalled();
  });

  for (const status of [401, 403, 500]) {
    it('does not retry application permission errors or uncertain mutations ' + status, () => {
      client.post('api/booking/bookingrequest', {}).subscribe({ error: error => expect(error.status).toBe(status) });
      http.expectOne(url('api/booking/bookingrequest')).flush(['Not authorized.'], { status, statusText: 'Failure' });
      http.expectNone(url('api/auth/refresh'));
      expect(auth.session).not.toBeNull();
    });
  }

  it('does not retry a network-failed mutation', () => {
    client.post('api/booking/bookingrequest', {}).subscribe({ error: error => expect(error.status).toBe(0) });
    http.expectOne(url('api/booking/bookingrequest')).error(new ProgressEvent('error'));
    http.expectNone(url('api/auth/refresh'));
  });

  it('routes a transient challenge failure to recovery and really retries the challenged token', () => {
    client.get('api/owner').subscribe({ error: () => {} });
    http.expectOne(url('api/owner')).flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne(url('api/auth/refresh')).flush(null, { status: 503, statusText: 'Unavailable' });
    expect(navigation.showFailure).toHaveBeenCalledWith('unavailable');
    auth.ensureSession().subscribe(value => expect(value).toBe('authenticated'));
    http.expectOne(url('api/auth/refresh')).flush(testSession({ accessToken: 'renewed' }));
  });

  it('does not replay a request after another account replaces the session', () => {
    client.get('api/owner').subscribe({ error: error => expect(error).toBeInstanceOf(SessionChangedError) });
    const req = http.expectOne(url('api/owner'));
    localStorage.setItem(sessionStorageKey, JSON.stringify(testSession({ userId: 2 })));
    req.flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectNone(url('api/auth/refresh'));
    expect(navigation.showFailure).not.toHaveBeenCalled();
  });

  it('does not expose a late protected response after definitive refresh rejection', () => {
    let lateError: unknown;
    client.get('api/late').subscribe({ error: error => lateError = error });
    const late = http.expectOne(url('api/late'));
    client.get('api/owner').subscribe({ error: () => {} });
    http.expectOne(url('api/owner')).flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne(url('api/auth/refresh')).flush(['Invalid refresh token.'], { status: 400, statusText: 'Bad Request' });
    late.flush({ privateData: 'old account' });
    expect(lateError).toBeInstanceOf(SessionChangedError);
    expect(navigation.showFailure).toHaveBeenCalledWith('signed-out');
  });

});
