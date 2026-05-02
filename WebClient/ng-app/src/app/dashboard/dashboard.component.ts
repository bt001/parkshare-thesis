import { Component } from '@angular/core';

import { UserService } from '../services/user.service';
import { TestService } from '../api/test/test.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent {
  pong?: string;
  error?: string;

  constructor(public user: UserService, private testService: TestService) {}

  pingApi() {
    this.pong = undefined;
    this.error = undefined;
    this.testService.ping().subscribe({
      next: (result) => (this.pong = result),
      error: (err) => (this.error = err.message || 'API call failed')
    });
  }
}
