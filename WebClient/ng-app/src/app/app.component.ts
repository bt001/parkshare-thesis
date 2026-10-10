import { Component, DestroyRef, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from './api/auth/auth.service';
import { SessionNavigationService } from './auth/session-navigation.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'ng-app';

  /** Drops protected content when another tab logs out or changes account. */
  constructor() {
    const router = inject(Router);
    const navigation = inject(SessionNavigationService);
    inject(AuthService).externalChanges.pipe(takeUntilDestroyed(inject(DestroyRef))).subscribe(change => {
      if (/^\/(login|register|session-recovery)(?:[/?#;]|$)/.test(router.url)) return;
      if (change === 'logout') navigation.showFailure('signed-out');
      else window.location.reload();
    });
  }
}
