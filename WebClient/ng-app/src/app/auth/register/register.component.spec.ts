import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../api/auth/auth.service';
import { RegisterComponent } from './register.component';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  const authService = jasmine.createSpyObj<AuthService>('AuthService', ['register', 'hasAuthenticatedSession']);
  let router: Router;

  beforeEach(async () => {
    authService.register.calls.reset();
    authService.hasAuthenticatedSession.calls.reset();
    authService.hasAuthenticatedSession.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl').and.resolveTo(true);

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('submits registration details and navigates to the dashboard on success', () => {
    authService.register.and.returnValue(of({
      userId: 1,
      userName: 'owner1',
      email: 'owner@example.com',
      role: 'Renter',
      roles: ['Renter'],
      name: 'Owner User',
      accessToken: 'token',
      accessTokenExpiresAt: '2099-01-01T00:00:00Z',
      tokenType: 'Bearer',
      refreshToken: 'refresh-token',
      refreshTokenExpiresAt: '2099-01-02T00:00:00Z'
    }));

    component.form.setValue({
      firstName: 'Owner',
      lastName: 'User',
      userName: 'owner1',
      email: 'owner@example.com',
      password: 'password123'
    });

    component.submit();

    expect(authService.register).toHaveBeenCalledWith({
      firstName: 'Owner',
      lastName: 'User',
      userName: 'owner1',
      email: 'owner@example.com',
      password: 'password123'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });

  it('surfaces API errors when registration fails', () => {
    authService.register.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 400,
      error: ['An account with this email already exists.']
    })));

    component.form.setValue({
      firstName: 'Owner',
      lastName: 'User',
      userName: 'owner1',
      email: 'owner@example.com',
      password: 'password123'
    });

    component.submit();
    fixture.detectChanges();

    expect(component.errorMessages).toEqual(['An account with this email already exists.']);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('An account with this email already exists.');
  });

  it('does not submit when the form is invalid', () => {
    component.form.setValue({
      firstName: '',
      lastName: 'User',
      userName: 'owner1',
      email: 'owner@example.com',
      password: 'password123'
    });

    component.submit();

    expect(authService.register).not.toHaveBeenCalled();
  });

  it('redirects authenticated users to the dashboard during construction', () => {
    authService.hasAuthenticatedSession.and.returnValue(true);

    const authenticatedFixture = TestBed.createComponent(RegisterComponent);
    authenticatedFixture.detectChanges();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });
});
