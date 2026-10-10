import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of, Subject } from 'rxjs';
import { AuthService, SessionOutcome } from '../api/auth/auth.service';
import { SessionRecoveryComponent } from './session-recovery.component';
import { safeReturnUrl } from './session-navigation.service';

describe('session recovery screen', () => {
  const auth = jasmine.createSpyObj<AuthService>('AuthService', ['ensureSession', 'logout']);
  beforeEach(() => {
    auth.logout.calls.reset();
    TestBed.configureTestingModule({ imports: [SessionRecoveryComponent], providers: [
      provideRouter([]), { provide: AuthService, useValue: auth },
      { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ returnUrl: '/bookings?status=0' }) } } }
    ] });
    spyOn(TestBed.inject(Router), 'navigateByUrl').and.resolveTo(true);
  });

  it('disables retry while waiting and returns to the preserved destination', () => {
    const pending = new Subject<SessionOutcome>();
    auth.ensureSession.and.returnValue(pending);
    const fixture = TestBed.createComponent(SessionRecoveryComponent);
    fixture.componentInstance.retry(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button').disabled).toBeTrue();
    pending.next('authenticated'); pending.complete();
    expect(TestBed.inject(Router).navigateByUrl).toHaveBeenCalledWith('/bookings?status=0');
    expect(fixture.componentInstance.isRecovering).toBeFalse();
  });

  it('stays available for retry after transient failure', () => {
    auth.ensureSession.and.returnValue(of('unavailable'));
    const fixture = TestBed.createComponent(SessionRecoveryComponent);
    fixture.componentInstance.retry();
    expect(TestBed.inject(Router).navigateByUrl).not.toHaveBeenCalled();
    expect(auth.logout).not.toHaveBeenCalled();
  });

  it('explicit sign-in clears retained credentials', () => {
    const fixture = TestBed.createComponent(SessionRecoveryComponent);
    fixture.componentInstance.signIn();
    expect(auth.logout).toHaveBeenCalled();
    const router = TestBed.inject(Router);
    const target = (router.navigateByUrl as jasmine.Spy).calls.mostRecent().args[0];
    expect(router.serializeUrl(target)).toBe('/login?returnUrl=%2Fbookings%3Fstatus%3D0');
  });

  for (const value of ['//evil.test', '/%2fexample.test', '/login', '/register?a=b', '/session-recovery', '/%73ession-recovery', '/a\\b', 'https://evil.test', '/%']) {
    it('rejects unsafe or recursive destination ' + value, () => expect(safeReturnUrl(value)).toBe('/dashboard'));
  }
});
