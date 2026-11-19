import { signal } from '@angular/core';
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

  const activity = { start: () => {}, count: signal<number | null>(3), ownerCount: signal<number | null>(2), renterCount: signal<number | null>(1), unavailable: signal(false) };

  beforeEach(async () => {
    activity.count.set(3); activity.ownerCount.set(2); activity.renterCount.set(1); activity.unavailable.set(false);
    authService.hasRole.and.returnValue(true);
    await TestBed.configureTestingModule({
      imports: [AppShellComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService }
      ]
    }).overrideComponent(AppShellComponent, { set: { providers: [{
      provide: NotificationStateService,
      useValue: activity
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
    expect(element.querySelector('a[href="/notifications"] .activity-badge')?.textContent).toBe('3');
    expect(element.querySelector('a[href="/bookings"] .activity-badge')?.textContent).toBe('1');
    expect(element.querySelector('.menu-toggle')?.getAttribute('aria-label')).toBe('Menu, outstanding activity');
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

  for (const source of ['count', 'ownerCount', 'renterCount'] as const) {
    it(`propagates ${source} activity through opening, closing and clearing the menu`, () => {
      activity.count.set(0); activity.ownerCount.set(0); activity.renterCount.set(0);
      fixture.detectChanges();
      const menu = fixture.nativeElement.querySelector('.menu-toggle') as HTMLButtonElement;
      expect(menu.querySelector('.activity-dot')).toBeNull();
      expect(menu.getAttribute('aria-label')).toBe('Menu');
      activity[source].set(1); fixture.detectChanges();
      expect(menu.querySelector('.activity-dot')).not.toBeNull();
      expect(menu.getAttribute('aria-label')).toBe('Menu, outstanding activity');
      menu.click(); fixture.detectChanges();
      expect(menu.getAttribute('aria-expanded')).toBe('true');
      expect(menu.querySelector('.activity-dot')).not.toBeNull();
      activity.unavailable.set(true); fixture.detectChanges();
      expect(menu.querySelector('.activity-dot')).not.toBeNull();
      menu.click(); fixture.detectChanges();
      expect(menu.getAttribute('aria-expanded')).toBe('false');
      expect(activity[source]()).toBe(1);
      activity[source].set(0); fixture.detectChanges();
      expect(menu.querySelector('.activity-dot')).toBeNull();
      expect(menu.getAttribute('aria-label')).toBe('Menu');
    });
  }

});
