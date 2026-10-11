import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { AuthService } from '../api/auth/auth.service';
import { AppShellComponent } from './app-shell.component';
import { NotificationStateService } from '../notifications/notification-state.service';

describe('AppShellComponent', () => {
  let component: AppShellComponent;
  let fixture: ComponentFixture<AppShellComponent>;
  let router: Router;
  const authService = jasmine.createSpyObj<AuthService>('AuthService', ['logout', 'hasRole'], {
    session: {
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
    }
  });

  beforeEach(async () => {
    authService.hasRole.and.returnValue(true);
    await TestBed.configureTestingModule({
      imports: [AppShellComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService }
      ]
    }).overrideComponent(AppShellComponent, { set: { providers: [{
      provide: NotificationStateService,
      useValue: { start: () => {}, count: () => 3, unavailable: () => false }
    }] } }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl').and.resolveTo(true);

    fixture = TestBed.createComponent(AppShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('clears the session and routes to login on logout', () => {
    component.logout();

    expect(authService.logout).toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });

  it('shows owner-aware navigation', () => {
    const element = fixture.nativeElement as HTMLElement;

    expect(element.textContent).toContain('ParkShare');
    expect(element.textContent).toContain('Browse parking');
    expect(element.querySelector('a[href="/bookings"]')?.textContent).toContain('My bookings');
    expect(element.textContent).toContain('Owner workspace');
    expect(element.querySelector('a[href="/notifications"]')?.textContent).toContain('Notifications (3)');
  });
});
