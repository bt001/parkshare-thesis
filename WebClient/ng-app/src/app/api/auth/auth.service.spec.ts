import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { AuthService, SessionChangedError } from './auth.service';
import { testSession } from './auth-test.helpers';
import { sessionStorageKey } from '../../services/auth-token-storage.service';

describe('AuthService recovery', () => {
  let http: HttpTestingController;
  const expired = () => testSession({ accessTokenExpiresAt: '2000-01-01T00:00:00Z' });
  function start(session = expired()): AuthService {
    localStorage.setItem(sessionStorageKey, JSON.stringify(session));
    return TestBed.inject(AuthService);
  }
  beforeEach(() => {
    localStorage.removeItem(sessionStorageKey);
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); localStorage.removeItem(sessionStorageKey); });

  it('keeps usable access without refreshing even when refresh has expired', () => {
    const auth = start(testSession({ refreshTokenExpiresAt: '2000-01-01' }));
    auth.ensureSession().subscribe(value => expect(value).toBe('authenticated'));
    http.expectNone('api/auth/refresh');
  });

  it('retains expired access and shares recovery among all subscribers', () => {
    const auth = start();
    expect(auth.isAuthenticated).toBeFalse();
    expect(auth.session).not.toBeNull();
    const outcomes: string[] = [];
    auth.ensureSession().subscribe(value => outcomes.push(value));
    auth.ensureSession().subscribe(value => outcomes.push(value));
    http.expectOne('api/auth/refresh').flush(testSession());
    expect(outcomes).toEqual(['authenticated', 'authenticated']);
    expect(auth.isAuthenticated).toBeTrue();
  });

  it('drops fully expired credentials without calling refresh', () => {
    const auth = start(testSession({ accessTokenExpiresAt: '2000-01-01', refreshTokenExpiresAt: '2000-01-02' }));
    auth.ensureSession().subscribe(value => expect(value).toBe('signed-out'));
    expect(localStorage.getItem(sessionStorageKey)).toBeNull();
  });

  for (const status of [0, 400, 401, 500, 503]) {
    it('retains credentials after a non-definitive refresh failure ' + status, () => {
      const auth = start();
      auth.ensureSession().subscribe(value => expect(value).toBe('unavailable'));
      const request = http.expectOne('api/auth/refresh');
      if (status === 0) request.error(new ProgressEvent('error'));
      else request.flush(['Unexpected service error.'], { status, statusText: 'Failure' });
      expect(auth.session?.refreshToken).toBe('refresh');
      auth.ensureSession().subscribe(value => expect(value).toBe('authenticated'));
      http.expectOne('api/auth/refresh').flush(testSession());
    });
  }

  it('clears a definitively rejected refresh token', () => {
    const auth = start();
    auth.ensureSession().subscribe(value => expect(value).toBe('signed-out'));
    http.expectOne('api/auth/refresh').flush(['Invalid refresh token.'], { status: 400, statusText: 'Bad Request' });
    expect(auth.session).toBeNull();
  });

  it('retains credentials on malformed successful responses', () => {
    const auth = start();
    auth.ensureSession().subscribe(value => expect(value).toBe('unavailable'));
    http.expectOne('api/auth/refresh').flush({ accessToken: 'bad' });
    expect(auth.session?.refreshToken).toBe('refresh');
  });

  it('times out after 15 seconds and allows an explicit retry', fakeAsync(() => {
    const auth = start();
    auth.ensureSession().subscribe(value => expect(value).toBe('unavailable'));
    const request = http.expectOne('api/auth/refresh');
    tick(15000);
    expect(request.cancelled).toBeTrue();
    expect(auth.session).not.toBeNull();
    auth.ensureSession().subscribe();
    http.expectOne('api/auth/refresh').flush(testSession());
  }));

  it('cancels refresh immediately on logout without resurrecting credentials', () => {
    const auth = start();
    const errors: unknown[] = [];
    auth.ensureSession().subscribe({ error: error => errors.push(error) });
    const request = http.expectOne('api/auth/refresh');
    auth.logout();
    expect(request.cancelled).toBeTrue();
    expect(errors[0]).toBeInstanceOf(SessionChangedError);
    expect(auth.session).toBeNull();
  });

  it('new login cancels old refresh and prevents an older login from winning', () => {
    const auth = start();
    auth.recover().subscribe({ error: error => expect(error).toBeInstanceOf(SessionChangedError) });
    const refresh = http.expectOne('api/auth/refresh');
    auth.login({ email: 'first', password: 'password' }).subscribe({ error: error => expect(error).toBeInstanceOf(SessionChangedError) });
    const first = http.expectOne('api/auth/login');
    auth.login({ email: 'second', password: 'password' }).subscribe();
    expect(refresh.cancelled).toBeTrue();
    expect(first.cancelled).toBeTrue();
    http.expectOne('api/auth/login').flush(testSession({ userId: 2 }));
    expect(auth.session?.userId).toBe(2);
  });

  it('adopts cross-tab replacement and logout while cancelling refresh', () => {
    const auth = start();
    const changes: string[] = [];
    auth.externalChanges.subscribe(value => changes.push(value));
    auth.recover().subscribe({ error: error => expect(error).toBeInstanceOf(SessionChangedError) });
    const request = http.expectOne('api/auth/refresh');
    localStorage.setItem(sessionStorageKey, JSON.stringify(testSession({ userId: 2 })));
    window.dispatchEvent(new StorageEvent('storage', { key: sessionStorageKey }));
    expect(request.cancelled).toBeTrue();
    expect(auth.session?.userId).toBe(2);
    localStorage.removeItem(sessionStorageKey);
    window.dispatchEvent(new StorageEvent('storage', { key: null }));
    expect(auth.session).toBeNull();
    expect(changes).toEqual(['account', 'logout']);
  });

  it('checks storage before committing even before the storage event arrives', () => {
    const auth = start();
    auth.recover().subscribe({ error: error => expect(error).toBeInstanceOf(SessionChangedError) });
    const request = http.expectOne('api/auth/refresh');
    localStorage.removeItem(sessionStorageKey);
    request.flush(testSession());
    expect(auth.session).toBeNull();
    expect(localStorage.getItem(sessionStorageKey)).toBeNull();
  });

  it('queues one owner-role refresh after older refresh work', () => {
    const auth = start();
    auth.recover().subscribe();
    const first = http.expectOne('api/auth/refresh');
    const roles: string[][] = [];
    auth.refresh().subscribe(session => roles.push(session.roles!));
    auth.refresh().subscribe(session => roles.push(session.roles!));
    first.flush(testSession());
    const second = http.expectOne('api/auth/refresh');
    second.flush(testSession({ roles: ['Renter', 'Owner'], accessToken: 'owner' }));
    expect(roles).toEqual([['Renter', 'Owner'], ['Renter', 'Owner']]);
    expect(auth.hasRole('Owner')).toBeTrue();
  });

  it('does not treat a failed optional role refresh as loss of valid access', () => {
    const auth = start(testSession());
    auth.refresh().subscribe({ error: () => {} });
    http.expectOne('api/auth/refresh').flush(null, { status: 503, statusText: 'Unavailable' });
    expect(auth.isAuthenticated).toBeTrue();
  });
});
