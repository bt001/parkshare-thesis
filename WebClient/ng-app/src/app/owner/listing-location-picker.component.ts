import { Component, DestroyRef, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription, switchMap, timer } from 'rxjs';
import { ListingLocation, StreetPlace } from '../api/listings/listing.models';
import { ListingService } from '../api/listings/listing.service';
import { LocationMapComponent } from './location-map.component';

/** Street, building number and explicit entrance confirmation for owner listings. */
@Component({
  selector: 'app-listing-location-picker', standalone: true, imports: [LocationMapComponent],
  templateUrl: './listing-location-picker.component.html', styleUrl: './listing-location-picker.component.scss'
})
export class ListingLocationPickerComponent implements OnChanges, OnDestroy {
  /** Existing location accepted without another lookup. */ @Input() saved: ListingLocation | null = null;
  /** Prevents editing during listing submission. */ @Input() disabled = false;
  /** Shows required-location feedback after a save attempt. */ @Input() showErrors = false;
  /** A confirmed location, or null when the draft becomes unconfirmed. */ @Output() readonly locationChanged = new EventEmitter<ListingLocation | null>();
  /** Authenticated address client. */
  private readonly service = inject(ListingService);
  /** Cancels work with the owning view. */
  private readonly destroyRef = inject(DestroyRef);
  /** Current debounced street request. */
  private search?: Subscription;
  /** Current structured address request. */
  private resolution?: Subscription;
  /** Rejects obsolete lookup responses. */
  private generation = 0;
  /** Whether the structured editor is open. */ editing = true;
  /** Street text draft. */ streetText = '';
  /** Building number draft. */ houseNumber = '';
  /** Selected structured street. */ selected: StreetPlace | null = null;
  /** Ordered street candidates. */ suggestions: StreetPlace[] = [];
  /** Keyboard-highlighted suggestion. */ activeIndex = -1;
  /** Street lookup progress. */ searching = false;
  /** Address lookup progress. */ locating = false;
  /** Whether street lookup completed with no matches. */ empty = false;
  /** Recoverable lookup feedback. */ error = '';
  /** Current address and entrance pin. */ draft: ListingLocation | null = null;
  /** Whether lookup is only approximate. */ approximate = false;
  /** Whether the owner deliberately placed the approximate pin. */ placed = false;
  /** Whether map tiles are available. */ mapReady = false;
  /** Explicit user confirmation. */ confirmed = false;

  /** Preserves loaded legacy addresses without parsing them. */
  ngOnChanges(changes: SimpleChanges): void { if (changes['saved'] && this.saved) this.editing = false; }
  /** Cancels all outstanding lookup work. */
  ngOnDestroy(): void { this.cancelRequests(); }
  /** Starts changing an existing location without losing the original. */
  changeLocation(): void { if (this.disabled) return; this.editing = true; this.locationChanged.emit(null); }
  /** Restores the original location and cancels pending responses. */
  cancelChange(): void {
    if (!this.saved || this.disabled) return;
    this.cancelRequests(); this.editing = false; this.draft = null; this.selected = null;
    this.streetText = ''; this.houseNumber = ''; this.suggestions = []; this.error = ''; this.confirmed = false;
    this.locationChanged.emit(this.saved);
  }
  /** Invalidates coordinates immediately and debounces fresh street suggestions. */
  streetChanged(value: string): void {
    if (this.disabled) return;
    this.cancelRequests(); this.streetText = value; this.selected = null; this.suggestions = []; this.activeIndex = -1;
    this.empty = false; this.invalidate();
    if (value.trim().length < 2 || value.length > 200) return;
    this.searching = true;
    const generation = this.generation;
    this.search = timer(400).pipe(switchMap(() => this.service.searchStreets(value.trim())), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (generation !== this.generation) return;
        this.searching = false; this.suggestions = result.items.filter(item => !!item.street && this.validPoint(item)); this.empty = !this.suggestions.length;
      },
      error: () => { if (generation === this.generation) { this.searching = false; this.error = 'Street suggestions could not be loaded. Retry street search.'; } }
    });
  }
  /** Selects a street while retaining the building-number draft. */
  selectStreet(street: StreetPlace): void {
    if (this.disabled) return;
    this.cancelRequests(); this.selected = street; this.streetText = street.label; this.suggestions = []; this.activeIndex = -1; this.empty = false; this.invalidate();
  }
  /** Supports accessible suggestion navigation and prevents accidental form submission. */
  streetKey(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      if (this.suggestions.length) this.activeIndex = (this.activeIndex + (event.key === 'ArrowDown' ? 1 : -1) + this.suggestions.length) % this.suggestions.length;
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (this.activeIndex >= 0 && this.suggestions[this.activeIndex]) this.selectStreet(this.suggestions[this.activeIndex]);
    } else if (event.key === 'Escape') { this.suggestions = []; this.activeIndex = -1; }
  }
  /** Clears confirmation and cancels an obsolete address request. */
  numberChanged(value: string): void {
    if (this.disabled) return;
    this.cancelRequests(); this.houseNumber = value; this.invalidate();
  }
  /** Resolves the selected address, retaining a deliberate manual-placement fallback. */
  locate(): void {
    if (this.disabled || !this.selected || !this.houseNumber.trim() || this.houseNumber.length > 20) return;
    this.cancelRequests(); this.invalidate(); this.locating = true;
    const generation = this.generation;
    const street = this.selected;
    const number = this.houseNumber.trim();
    this.resolution = this.service.resolveAddress(street, number).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (generation !== this.generation) return;
        this.locating = false;
        const normal = (value: string): string => value.trim().toLocaleLowerCase().replace(/[–—]/g, '-');
        const exact = result.items.find(item => this.validPoint(item) && !!item.houseNumber && normal(item.houseNumber) === normal(number)
          && !!item.street && normal(item.street) === normal(street.street));
        this.approximate = !exact;
        const point = exact ?? street;
        const address = exact?.label ?? [street.street + ' ' + number, street.city, street.postcode, 'Greece'].filter(Boolean).join(', ');
        if (address.length > 200) { this.error = 'The address exceeds 200 characters. Choose a shorter street/locality match.'; return; }
        this.draft = { address, latitude: point.latitude, longitude: point.longitude };
        this.placed = false; this.mapReady = false;
      },
      error: () => { if (generation === this.generation) { this.locating = false; this.error = 'Address lookup is unavailable. Your draft is kept; retry Locate address.'; } }
    });
  }
  /** Moves the entrance and requires a fresh confirmation. */
  movePin(point: { latitude: number; longitude: number }): void {
    if (this.disabled || !this.draft || !this.validPoint(point)) return;
    this.draft = { ...this.draft, ...point }; this.placed = true; this.confirmed = false; this.locationChanged.emit(null);
  }
  /** Blocks confirmation if the map stops displaying successfully. */
  mapAvailability(ready: boolean): void {
    this.mapReady = ready;
    if (!ready && this.confirmed) { this.confirmed = false; this.locationChanged.emit(null); }
  }
  /** Accepts the address and the explicitly reviewed entrance. */
  confirm(): void {
    if (this.disabled || !this.draft || !this.mapReady || (this.approximate && !this.placed)) return;
    this.confirmed = true; this.locationChanged.emit({ ...this.draft });
  }
  /** Clears resolved state after address edits. */
  private invalidate(): void { this.draft = null; this.confirmed = false; this.mapReady = false; this.error = ''; this.locationChanged.emit(null); }
  /** Cancels subscriptions before accepting another input generation. */
  private cancelRequests(): void { this.generation++; this.search?.unsubscribe(); this.resolution?.unsubscribe(); this.searching = false; this.locating = false; }
  /** Guards provider coordinates before displaying or saving them. */
  private validPoint(point: { latitude: number; longitude: number }): boolean {
    return Number.isFinite(point.latitude) && Number.isFinite(point.longitude) && Math.abs(point.latitude) <= 90 && Math.abs(point.longitude) <= 180;
  }
}
