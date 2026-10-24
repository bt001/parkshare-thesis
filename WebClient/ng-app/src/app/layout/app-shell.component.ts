import { Component, ElementRef, ViewChild, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

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
  constructor() {
    this.notifications.start();
    this.router.events.pipe(takeUntilDestroyed()).subscribe(event => {
      if (event instanceof NavigationEnd) {
        this.closeNavigation();
        if (this.account) this.account.nativeElement.open = false;
      }
    });
  }

  /** Account disclosure closed when navigating between workflows. */
  @ViewChild('account') private account?: ElementRef<HTMLDetailsElement>;

  readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  /** Whether compact navigation is expanded. */
  navigationOpen = false;

  /** Dismisses compact navigation and optionally restores keyboard focus. */
  closeNavigation(trigger?: HTMLButtonElement): void {
    this.navigationOpen = false;
    trigger?.focus();
  }

  /** Returns focus to the disclosure that Escape dismisses. */
  dismissHeader(menuTrigger: HTMLButtonElement, account: HTMLDetailsElement, accountTrigger: HTMLElement): void {
    if (account.open) {
      account.open = false;
      accountTrigger.focus();
    } else if (this.navigationOpen) {
      this.closeNavigation(menuTrigger);
    }
  }

  /** Ends the current session and returns to login. */
  logout(): void {
    this.authService.logout();
    void this.router.navigateByUrl('/login');
  }
}
