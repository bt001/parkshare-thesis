import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ArrivalPickerComponent } from './arrival-picker.component';
import { StayDurationComponent } from './stay-duration.component';
import { departureForStay, stayMinutes } from '../services/booking-time';

/** A period edit, retaining the existing API fields and incomplete duration intent. */
export interface BookingPeriodEdit {
  /** Selected offset-free Athens arrival. */
  startDateTime: string;
  /** Calculated offset-free Athens departure. */
  endDateTime: string;
  /** Whether duration intent still requires an arrival or a valid length. */
  incomplete: boolean;
}

/** Shared renter arrival and stay controls, adapting to the existing period contract. */
@Component({
  selector: 'app-booking-period', standalone: true, host: { '[class.compact]': 'compact' },
  imports: [ReactiveFormsModule, DatePipe, DecimalPipe, ArrivalPickerComponent, StayDurationComponent],
  template: `
    <div class="period-fields">
      <div><label [for]="prefix + '-arrival'">Arrival</label><app-arrival-picker [inputId]="prefix + '-arrival'" [compact]="compact" [formControl]="arrival" /></div>
      <div><label [for]="prefix + '-duration'">Length of stay</label><app-stay-duration [inputId]="prefix + '-duration'" [compact]="compact" [formControl]="duration" /></div>
    </div>
    @if (departure && !compact) {
      <div class="period-summary" aria-live="polite"><span>Departure <strong>{{ departure | date:'EEE, d MMM y, HH:mm' }}</strong></span>
        @if (pricePerHour !== null && duration.value) { <span>Estimated total <strong>{{ pricePerHour * duration.value / 60 | number:'1.2-2' }} €</strong></span> }
      </div>
    }
    @if (endDateTime && !departure) { <p class="period-error">Saved departure: {{ endDateTime }}. Choose a valid arrival and length of stay.</p> }
  `,
  styles: [`
    :host { display: block; min-width: 0; }
    .period-fields { display: grid; grid-template-columns: repeat(auto-fit, minmax(min(100%, 15rem), 1fr)); gap: .8rem; }
    .period-fields > div { min-width: 0; }
    label { display: block; font-size: .85rem; font-weight: 600; margin-bottom: .35rem; }
    .period-summary { display: grid; gap: .35rem; margin-top: .8rem; padding: .75rem; background: var(--soft); border-radius: .65rem; font-size: .8rem; color: var(--muted); }
    .period-summary span { display: flex; flex-wrap: wrap; justify-content: space-between; gap: .3rem .65rem; }.period-summary strong { color: var(--ink); }
    :host(.compact) .period-fields { grid-template-columns: 34fr 22fr; gap: 0; }
    :host(.compact) .period-fields > div { min-height: 64px; padding: .45rem .9rem; border-left: 1px solid var(--line); }
    :host(.compact) label { font-size: .7rem; color: var(--muted); margin-bottom: .1rem; }
    @media(max-width: 599px) { :host(.compact) .period-fields { grid-template-columns: 1fr; } :host(.compact) .period-fields > div { border-left: 0; border-top: 1px solid var(--line); } }
    .period-error { color: var(--danger); font-size: .8rem; }
  `]
})
export class BookingPeriodComponent implements OnChanges {
  /** Uses the compact discovery-bar layout without changing booking forms. */
  @Input() compact = false;
  /** Identifier namespace for labelled form controls. */
  @Input() prefix = 'parking';
  /** Existing period arrival, including invalid saved values for correction. */
  @Input() startDateTime = '';
  /** Existing period departure, retained for URL and API compatibility. */
  @Input() endDateTime = '';
  /** Explicit reset/restoration revision, including when a blank period remains blank. */
  @Input() revision = 0;
  /** Optional listing rate used for the booking estimate. */
  @Input() pricePerHour: number | null = null;
  /** Publishes user edits without applying a discovery search. */
  @Output() periodChange = new EventEmitter<BookingPeriodEdit>();
  /** Arrival control shared with the accessible picker. */
  readonly arrival = new FormControl('', { nonNullable: true });
  /** Selected length in minutes; null represents an incomplete selection. */
  readonly duration = new FormControl<number | null>(null);
  /** Last emitted values distinguish parent echoes from external restoration. */
  private emitted?: { start: string; end: string; revision: number };
  /** Connects local controls without changing parent state until user interaction. */
  constructor() {
    this.arrival.valueChanges.subscribe(start => {
      if (start && this.duration.value === null && !this.startDateTime && !this.endDateTime) this.duration.setValue(60, { emitEvent: false });
      if (!start) this.duration.setValue(null, { emitEvent: false });
      this.publish();
    });
    this.duration.valueChanges.subscribe(() => this.publish());
  }
  /** Restores external periods while preserving an in-progress custom-duration edit. */
  ngOnChanges(): void {
    if (this.emitted?.start === this.startDateTime && this.emitted.end === this.endDateTime && this.emitted.revision === this.revision) return;
    this.arrival.setValue(this.startDateTime, { emitEvent: false });
    this.duration.setValue(stayMinutes(this.startDateTime, this.endDateTime), { emitEvent: false });
  }
  /** Calculates departure without depending on the browser timezone. */
  get departure(): string { return departureForStay(this.arrival.value, this.duration.value); }
  /** Emits a period and preserves partial selection intent for parent validation. */
  private publish(): void {
    const start = this.arrival.value, end = this.departure;
    this.emitted = { start, end, revision: this.revision };
    this.periodChange.emit({ startDateTime: start, endDateTime: end, incomplete: (!!start || this.duration.value !== null || this.duration.invalid) && !end });
  }
}
