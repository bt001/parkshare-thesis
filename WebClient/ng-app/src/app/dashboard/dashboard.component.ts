import { Component } from '@angular/core';

import { AuthService } from '../api/auth/auth.service';
import { UserService } from '../services/user.service';
import { TestService } from '../api/test/test.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  pong?: string;
  error?: string;

  constructor(
    public user: UserService,
    public auth: AuthService,
    private testService: TestService
  ) {}

  pingApi(): void {
    this.pong = undefined;
    this.error = undefined;
    this.testService.ping().subscribe({
      next: (result) => (this.pong = result),
      error: (err) => (this.error = err.message || 'API call failed')
    });
  }
}
