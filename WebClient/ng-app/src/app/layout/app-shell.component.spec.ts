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
  it('dismisses compact navigation and restores focus on Escape', () => {
    const trigger = fixture.nativeElement.querySelector('.menu-toggle') as HTMLButtonElement;
    spyOn(trigger, 'focus');
    component.navigationOpen = true;
    component.closeNavigation(trigger);
    expect(component.navigationOpen).toBeFalse();
    expect(trigger.focus).toHaveBeenCalled();
  });
  it('closes compact navigation after selecting a destination', () => {
    component.navigationOpen = true;
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.shell-nav').click();
    expect(component.navigationOpen).toBeFalse();
  });

  it('returns Escape focus to the account disclosure', () => {
    const account = fixture.nativeElement.querySelector('details') as HTMLDetailsElement;
    const summary = account.querySelector('summary')!;
    const menu = fixture.nativeElement.querySelector('.menu-toggle') as HTMLButtonElement;
    spyOn(summary, 'focus');
    account.open = true;
    component.dismissHeader(menu, account, summary);
    expect(account.open).toBeFalse();
    expect(summary.focus).toHaveBeenCalled();
  });

});
