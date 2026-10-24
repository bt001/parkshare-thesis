import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../api/auth/auth.service';

/** Provides shortcuts to the signed-in user's parking workflows. */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  /** Current account used for the welcome and owner-aware actions. */
  readonly auth = inject(AuthService);
}
