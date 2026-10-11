import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { NotificationStateService } from '../notifications/notification-state.service';
import { AuthService } from '../api/auth/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  providers: [NotificationStateService],
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss'
})
export class AppShellComponent {
  /** Unread state owned by this authenticated shell. */
  readonly notifications = inject(NotificationStateService);
  /** Starts notification refresh for the active account. */
  constructor() { this.notifications.start(); }

  readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    this.authService.logout();
    void this.router.navigateByUrl('/login');
  }
}
