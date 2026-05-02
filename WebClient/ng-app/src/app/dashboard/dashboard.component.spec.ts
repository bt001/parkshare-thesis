import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AuthService } from '../api/auth/auth.service';
import { TestService } from '../api/test/test.service';
import { DashboardComponent } from './dashboard.component';
import { UserService } from '../services/user.service';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  const authService = jasmine.createSpyObj<AuthService>('AuthService', [], {
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
    testService.ping.and.returnValue(of('pong'));

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
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
    expect(compiled.textContent).toContain('owner@example.com');
    expect(compiled.textContent).toContain('Owner');
  });
});
