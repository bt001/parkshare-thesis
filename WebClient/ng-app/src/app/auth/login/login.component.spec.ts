import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../api/auth/auth.service';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  const authService = jasmine.createSpyObj<AuthService>('AuthService', ['login', 'hasAuthenticatedSession']);
  let router: Router;

  beforeEach(async () => {
    authService.hasAuthenticatedSession.and.returnValue(false);

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
});
