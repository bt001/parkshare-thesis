import { NotificationStateService } from '../notifications/notification-state.service';
import { ActivityBadgeComponent } from '../shared/activity-badge.component';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../api/auth/auth.service';

/** Provides shortcuts to the signed-in user's parking workflows. */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [ActivityBadgeComponent, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  /** Activity state inherited from the authenticated shell. */
  readonly activity = inject(NotificationStateService, { optional: true });
  /** Distinct owned bookings requiring attention. */
  get ownerAttention(): number | null { return this.activity?.ownerCount?.() ?? null; }
  /** Distinct rented bookings requiring attention. */
  get renterAttention(): number | null { return this.activity?.renterCount?.() ?? null; }

  /** Current account used for the welcome and owner-aware actions. */
  readonly auth = inject(AuthService);
}
