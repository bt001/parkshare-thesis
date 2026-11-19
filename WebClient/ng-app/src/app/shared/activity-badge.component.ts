import { Component, Input } from '@angular/core';

/** Compact accessible activity count, with an explicit stale-state hint. */
@Component({
  selector: 'app-activity-badge', standalone: true,
  template: `@if (count || stale) { <span class="activity-badge" [class.stale]="stale" [attr.aria-label]="label" [attr.title]="label">{{ count ? (count > 99 ? '99+' : count) : '?' }}</span> }`,
  styles: [`.activity-badge { display:inline-flex; align-items:center; justify-content:center; min-width:1.3rem; padding:.1rem .35rem; margin-inline-start:.3rem; border-radius:999px; background:#b42335; color:white; font-size:.72rem; font-weight:700; line-height:1.2; vertical-align:middle; } .stale { outline:2px dotted currentColor; outline-offset:2px; }`]
})
export class ActivityBadgeComponent {
  /** Count across the corresponding workflow; null means not yet known. */
  @Input() count: number | null | undefined = null;
  /** Description of the counted items. */
  @Input() description = 'bookings need attention';
  /** Whether a prior count is stale or unavailable. */
  @Input() stale = false;
  /** Full accessible count, without visual truncation. */
  get label(): string { return `${this.count ?? 'Unknown'} ${this.description}${this.stale ? '; updates temporarily unavailable' : ''}`; }
}
