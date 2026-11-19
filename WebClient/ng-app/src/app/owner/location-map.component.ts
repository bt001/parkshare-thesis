import { isPlatformBrowser } from '@angular/common';
import { AfterViewInit, Component, ElementRef, EventEmitter, Input, OnChanges, OnDestroy, Output, PLATFORM_ID, ViewChild, inject } from '@angular/core';
import type { Map as LeafletMap, Marker } from 'leaflet';

/** Browser-only map for selecting a parking entrance. */
@Component({
  selector: 'app-location-map', standalone: true,
  template: `
    <div #canvas class="location-map" role="region" aria-label="Parking entrance map. Use arrow keys to pan and plus or minus to zoom."></div>
    @if (failed) { <p role="alert">The map could not be displayed. Retry before confirming the location.</p><button type="button" (click)="retry()">Retry map</button> }
    @else if (!ready) { <p role="status">Loading map…</p> }
    @if (editable) { <button type="button" [disabled]="!ready || failed || disabled" (click)="useCentre()">Use map centre</button> }
  `,
  styles: `.location-map { height: 320px; width: 100%; border-radius: 12px; z-index: 0; margin: 1rem 0; } :host { display: block; } button { min-height: 2.5rem; padding: .55rem .8rem; border: 1px solid var(--line); border-radius: .45rem; color: var(--ink); background: var(--surface); font-weight: 600; cursor: pointer; } button:disabled { opacity: .55; cursor: not-allowed; } @media(max-width: 480px) { .location-map { height: 270px; } }`
})
export class LocationMapComponent implements AfterViewInit, OnChanges, OnDestroy {
  /** Current pin location. */ @Input() point!: { latitude: number; longitude: number };
  /** Allows moving the entrance pin. */ @Input() editable = true;
  /** Prevents changes while saving. */ @Input() disabled = false;
  /** Explicit pin placement. */ @Output() readonly pointChanged = new EventEmitter<{ latitude: number; longitude: number }>();
  /** Whether the map has successfully displayed tiles. */ @Output() readonly available = new EventEmitter<boolean>();
  /** Map container owned by this component. */
  @ViewChild('canvas') private canvas?: ElementRef<HTMLElement>;
  /** Runtime rendering platform. */
  private readonly platform = inject(PLATFORM_ID);
  /** Active Leaflet instance. */
  private map?: LeafletMap;
  /** Current entrance marker. */
  private marker?: Marker;
  /** Responsive container observer. */
  private observer?: ResizeObserver;
  /** Blocks callbacks after teardown. */
  private destroyed = false;
  /** Invalidates obsolete asynchronous map work. */
  private generation = 0;
  /** Bounded initial tile wait. */
  private deadline?: ReturnType<typeof setTimeout>;
  /** Tile readiness. */ ready = false;
  /** Recoverable map failure. */ failed = false;

  /** Initializes only in the browser. */
  ngAfterViewInit(): void { if (isPlatformBrowser(this.platform)) void this.initialize(); }
  /** Keeps pin and editing state synchronized with the form. */
  ngOnChanges(): void {
    if (this.marker && this.point) {
      const current = this.marker.getLatLng();
      if (current.lat !== this.point.latitude || current.lng !== this.point.longitude) {
        this.marker.setLatLng([this.point.latitude, this.point.longitude]);
        this.map?.panTo([this.point.latitude, this.point.longitude]);
      }
      if (this.editable && !this.disabled) this.marker.dragging?.enable(); else this.marker.dragging?.disable();
    }
  }
  /** Disposes map listeners and pending initialization. */
  ngOnDestroy(): void { this.destroyed = true; clearTimeout(this.deadline); this.generation++; this.observer?.disconnect(); this.map?.remove(); }
  /** Recreates the map and retries failed tiles. */
  retry(): void { clearTimeout(this.deadline); this.generation++; this.observer?.disconnect(); this.map?.remove(); this.map = undefined; void this.initialize(); }
  /** Selects the centre after keyboard or touch panning. */
  useCentre(): void { const centre = this.map?.getCenter(); if (centre) this.place(centre.lat, centre.lng); }
  /** Applies a deliberate pin movement, wrapping repeated map worlds. */
  private place(latitude: number, longitude: number): void {
    if (!this.editable || this.disabled || !this.ready || this.failed) return;
    const point = { latitude: Math.max(-90, Math.min(90, latitude)), longitude: ((longitude + 180) % 360 + 360) % 360 - 180 };
    this.marker?.setLatLng([point.latitude, point.longitude]);
    this.pointChanged.emit(point);
  }
  /** Loads Leaflet lazily and observes responsive map dimensions. */
  private async initialize(): Promise<void> {
    const generation = ++this.generation;
    this.ready = false; this.failed = false; this.available.emit(false);
    this.deadline = setTimeout(() => {
      if (!this.ready && !this.destroyed && generation === this.generation) { this.failed = true; this.available.emit(false); }
    }, 12000);
    try {
      const leaflet = await import('leaflet/dist/leaflet-src.esm.js');
      if (this.destroyed || generation !== this.generation || !this.canvas || !this.point) return;
      this.map = leaflet.map(this.canvas.nativeElement, { trackResize: false }).setView([this.point.latitude, this.point.longitude], 18);
      leaflet.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19, attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
      }).on('tileload', () => {
        if (!this.failed && !this.destroyed && generation === this.generation) { clearTimeout(this.deadline); this.ready = true; this.available.emit(true); }
      }).on('tileerror', () => {
        if (!this.destroyed && generation === this.generation) { this.failed = true; this.available.emit(false); }
      }).addTo(this.map);
      this.marker = leaflet.marker([this.point.latitude, this.point.longitude], {
        draggable: this.editable && !this.disabled, keyboard: true, title: 'Parking entrance',
        icon: leaflet.divIcon({ className: 'entrance-pin', html: '<span aria-hidden="true" style="display:block;width:24px;height:24px;border:3px solid white;border-radius:50%;background:#125a46;box-shadow:0 2px 8px #0008"></span>', iconSize: [30, 30], iconAnchor: [15, 15] })
      }).addTo(this.map);
      this.marker.on('dragend', () => { const point = this.marker!.getLatLng(); this.place(point.lat, point.lng); });
      this.map.on('click', event => this.place(event.latlng.lat, event.latlng.lng));
      this.observer = new ResizeObserver(() => this.map?.invalidateSize({ pan: false }));
      this.observer.observe(this.canvas.nativeElement);
    } catch {
      if (!this.destroyed && generation === this.generation) { this.failed = true; this.available.emit(false); }
    }
  }
}
