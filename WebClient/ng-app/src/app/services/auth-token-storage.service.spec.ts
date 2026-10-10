import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AuthTokenStorageService, sessionStorageKey } from './auth-token-storage.service';
import { testSession } from '../api/auth/auth-test.helpers';

describe('AuthTokenStorageService', () => {
  afterEach(() => localStorage.removeItem(sessionStorageKey));

  for (const value of ['{', 'null', '[]', '{}', JSON.stringify(testSession({ accessToken: '' })), JSON.stringify(testSession({ userId: -1 })), JSON.stringify(testSession({ refreshTokenExpiresAt: 'invalid' })), JSON.stringify({ ...testSession(), roles: [123] })]) {
    it('discards malformed persisted data: ' + value.slice(0, 30), () => {
      localStorage.setItem(sessionStorageKey, value);
      expect(TestBed.inject(AuthTokenStorageService).getSession()).toBeNull();
      expect(localStorage.getItem(sessionStorageKey)).toBeNull();
    });
  }

  it('does not access localStorage during server rendering', () => {
    TestBed.configureTestingModule({ providers: [{ provide: PLATFORM_ID, useValue: 'server' }] });
    const read = spyOn(Storage.prototype, 'getItem').and.throwError('Unavailable');
    const service = TestBed.inject(AuthTokenStorageService);
    service.setSession(testSession());
    expect(service.getSession()).toBeNull();
    service.clear();
    expect(read).not.toHaveBeenCalled();
  });

  it('keeps memory state after storage denies reads or writes', () => {
    const service = TestBed.inject(AuthTokenStorageService);
    spyOn(Storage.prototype, 'getItem').and.throwError('Unavailable');
    spyOn(Storage.prototype, 'setItem').and.throwError('Unavailable');
    spyOn(Storage.prototype, 'removeItem').and.throwError('Unavailable');
    expect(service.getSession()).toBeNull();
    service.setSession(testSession());
    expect(service.getSession()?.accessToken).toBe('access');
    service.clear();
    expect(service.getSession()).toBeNull();
    (Storage.prototype.removeItem as jasmine.Spy).and.callThrough();
  });
});
