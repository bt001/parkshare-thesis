import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';

import { AuthService, SessionOutcome } from '../../api/auth/auth.service';
import { AuthSession } from '../../api/auth/auth.models';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  const authService = jasmine.createSpyObj<AuthService>('AuthService', ['login', 'hasAuthenticatedSession', 'ensureSession']);
  let router: Router;

  beforeEach(async () => {
    authService.hasAuthenticatedSession.and.returnValue(false);
    authService.ensureSession.and.returnValue(of('signed-out'));

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl').and.resolveTo(true);

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('submits credentials and navigates to the dashboard on success', () => {
    authService.login.and.returnValue(of({
      userId: 1,
      userName: 'owner1',
      email: 'owner@example.com',
      role: 'Owner',
      name: 'Owner User',
      accessToken: 'token',
      accessTokenExpiresAt: '2099-01-01T00:00:00Z',
      tokenType: 'Bearer',
      refreshToken: 'refresh-token',
      refreshTokenExpiresAt: '2099-01-02T00:00:00Z'
    }));

    component.form.setValue({
      email: 'owner@example.com',
      password: 'password123'
    });

    component.submit();

    expect(authService.login).toHaveBeenCalledWith({
      email: 'owner@example.com',
      password: 'password123'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });

  it('surfaces API errors when login fails', () => {
    authService.login.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 400,
      error: ['Invalid credentials.']
    })));

    component.form.setValue({
      email: 'owner@example.com',
      password: 'wrong-password'
    });

    component.submit();
    fixture.detectChanges();

    expect(component.errorMessages).toEqual(['Invalid credentials.']);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Invalid credentials.');
  });
  for (const destination of ['/bookings', '//example.test', '/login', '/register?returnUrl=/bookings', 'https://example.test']) {
    it(`uses a safe return destination after login: ${destination}`, () => {
      spyOnProperty(TestBed.inject(ActivatedRoute).snapshot, 'queryParamMap', 'get').and.returnValue(convertToParamMap({ returnUrl: destination }));
      authService.login.and.returnValue(of({} as AuthSession));
      component.form.setValue({ email: 'renter@example.test', password: 'password123' });
      component.submit();
      expect(router.navigateByUrl).toHaveBeenCalledWith(destination === '/bookings' ? destination : '/dashboard');
    });
  }

  it('blocks submission while initial recovery is pending', () => {
    const pending = new Subject<SessionOutcome>();
    authService.ensureSession.and.returnValue(pending);
    authService.login.calls.reset();
    const recoveringFixture = TestBed.createComponent(LoginComponent);
    const recovering = recoveringFixture.componentInstance;
    recovering.form.setValue({ email: 'test@example.test', password: 'password123' });
    recovering.submit();
    expect(authService.login).not.toHaveBeenCalled();
    expect(recovering.isRecovering).toBeTrue();
    pending.next('signed-out'); pending.complete();
    expect(recovering.isRecovering).toBeFalse();
  });

  it('opens recovery instead of submitting new credentials during an outage', () => {
    authService.ensureSession.and.returnValue(of('unavailable'));
    TestBed.createComponent(LoginComponent);
    const destination = (router.navigateByUrl as jasmine.Spy).calls.mostRecent().args[0];
    expect(router.serializeUrl(destination)).toBe('/session-recovery?returnUrl=%2Fdashboard');
  });

});
