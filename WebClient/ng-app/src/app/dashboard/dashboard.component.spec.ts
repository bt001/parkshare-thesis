import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../api/auth/auth.service';
import { TestService } from '../api/test/test.service';
import { DashboardComponent } from './dashboard.component';
import { UserService } from '../services/user.service';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  const authService = jasmine.createSpyObj<AuthService>('AuthService', ['hasRole'], {
    session: {
      name: 'Owner User',
      userName: 'owner1',
      email: 'owner@example.com',
      role: 'Owner',
      accessToken: 'token',
      accessTokenExpiresAt: '2099-01-01T00:00:00Z',
      tokenType: 'Bearer',
      refreshToken: 'refresh-token',
      refreshTokenExpiresAt: '2099-01-02T00:00:00Z',
      userId: 1
    },
    isAuthenticated: true
  });
  const testService = jasmine.createSpyObj<TestService>('TestService', ['ping']);

  beforeEach(async () => {
    authService.hasRole.and.returnValue(true);
    testService.ping.and.returnValue(of('pong'));

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        UserService,
        { provide: AuthService, useValue: authService },
        { provide: TestService, useValue: testService }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the authenticated user summary', () => {
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Owner User');
    expect(compiled.querySelector('a[href="/listings"]')?.textContent).toContain('Find parking');
    expect(compiled.querySelector('a[href="/bookings"]')).not.toBeNull();
    expect(compiled.querySelector('a[href="/owner/listings"]')).not.toBeNull();
    expect(compiled.textContent).not.toContain('Developer verification');
    expect(testService.ping).not.toHaveBeenCalled();
  });
  it('offers first-listing navigation to renters without owner access', () => {
    authService.hasRole.and.returnValue(false);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('a[href="/owner/listings"]')?.textContent).toContain('List your space');
  });

});
