import { isPlatformBrowser } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  inject,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  PLATFORM_ID,
  SimpleChanges,
  ViewChild
} from '@angular/core';
import type { LayerGroup, Marker, Map as LeafletMap } from 'leaflet';

import { SearchListingItem } from '../api/listings/listing.models';

@Component({
  selector: 'app-listing-map',
  standalone: true,
  templateUrl: './listing-map.component.html',
  styleUrl: './listing-map.component.scss'
})
export class ListingMapComponent implements AfterViewInit, OnChanges, OnDestroy {
  private readonly platformId = inject(PLATFORM_ID);

  @ViewChild('mapContainer') private mapContainer?: ElementRef<HTMLElement>;
  @Input() listings: SearchListingItem[] = [];
  /** Applied search circle; keeps an empty map centred on the requested place. */
  @Input() searchArea: { latitude: number; longitude: number } | null = null;
  /** Great-circle search distance in kilometres. */
  @Input() radiusKm = 5;
  /** Explicit search action after the user pans or zooms. */
  @Output() readonly areaSelected = new EventEmitter<{ latitude: number; longitude: number }>();
  @Output() readonly listingSelected = new EventEmitter<SearchListingItem>();

  /** Selected card or marker, shared by the discovery view. */
  @Input() selectedListingId: number | null = null;
  /** Marker references allow selection updates without refitting the map. */
  private readonly markers = new Map<number, Marker>();

  isTileUnavailable = false;
  private leaflet?: typeof import('leaflet/dist/leaflet-src.esm.js');
  private map?: LeafletMap;
  private markerLayer?: LayerGroup;
  private isDestroyed = false;
  private resizeObserver?: ResizeObserver;

  ngAfterViewInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      void this.initializeMap();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['listings'] || changes['searchArea'] || changes['radiusKm']) && this.map) {
      this.renderMarkers();
    }
    if (changes['selectedListingId']) this.updateSelection(true);
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
    this.resizeObserver?.disconnect();
    this.map?.remove();
  }

  /** Emits the current centre without querying on every map movement. */
  searchCentre(): void {
    if (!this.map) return;
    const centre = this.map.getCenter();
    this.areaSelected.emit({ latitude: Math.max(-90, Math.min(90, centre.lat)), longitude: ((centre.lng + 180) % 360 + 360) % 360 - 180 });
  }

  private async initializeMap(): Promise<void> {
    const leaflet = await import('leaflet/dist/leaflet-src.esm.js');
    if (this.isDestroyed || !this.mapContainer) {
      return;
    }

    this.leaflet = leaflet;
    this.map = leaflet.map(this.mapContainer.nativeElement, {
      zoomControl: true,
      attributionControl: true
    }).setView([37.9838, 23.7275], 12);

    leaflet.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    })
      .on('tileerror', () => (this.isTileUnavailable = true))
      .addTo(this.map);

    this.resizeObserver = new ResizeObserver(() => this.map?.invalidateSize());
    this.resizeObserver.observe(this.mapContainer.nativeElement);
    this.markerLayer = leaflet.layerGroup().addTo(this.map);
    this.renderMarkers();
  }

  private renderMarkers(): void {
    if (!this.leaflet || !this.map || !this.markerLayer) {
      return;
    }

    this.markerLayer.clearLayers();
    this.markers.clear();
    const validListings = this.listings.filter((listing) => this.hasValidCoordinates(listing));
    const coordinates: Array<[number, number]> = [];

    for (const listing of [...validListings].reverse()) {
      const coordinate: [number, number] = [listing.latitude, listing.longitude];
      coordinates.push(coordinate);

      const price = new Intl.NumberFormat('ro-RO', { maximumFractionDigits: 2 }).format(listing.pricePerHour);
      const icon = this.leaflet.divIcon({
        className: 'listing-price-marker-shell',
        html: `<span class="listing-map-marker" data-listing-id="${listing.id}">${price} RON</span>`,
        iconAnchor: [36, 16]
      });

      const marker = this.leaflet.marker(coordinate, {
        icon,
        title: listing.title,
        alt: `${listing.title}, ${price} RON per hour`,
        riseOnHover: true
      })
        .on('click', () => this.listingSelected.emit(listing))
        .addTo(this.markerLayer);
      this.markers.set(listing.id, marker);
      const element = marker.getElement();
      element?.setAttribute('aria-label', `${listing.title}, ${price} RON per hour`);
      element?.addEventListener('keydown', event => {
        if (event.key === ' ' || event.key === 'Enter') {
          event.preventDefault();
          this.listingSelected.emit(listing);
        }
      });
    }

    this.updateSelection(false);
    this.map.invalidateSize();
    if (this.searchArea) {
      const circle = this.leaflet.circle([this.searchArea.latitude, this.searchArea.longitude], {
        radius: this.radiusKm * 1000, color: '#17685e', weight: 1, fillOpacity: 0.04, interactive: false
      }).addTo(this.markerLayer);
      this.map.fitBounds(circle.getBounds(), { padding: [20, 20], maxZoom: 16 });
    } else if (coordinates.length === 1) {
      this.map.setView(coordinates[0], 15);
    } else if (coordinates.length > 1) {
      this.map.fitBounds(this.leaflet.latLngBounds(coordinates), {
        padding: [40, 40],
        maxZoom: 15
      });
    }
  }

  /** Highlights selection in place and brings an off-screen selected marker into view. */
  private updateSelection(pan: boolean): void {
    for (const [id, marker] of this.markers) {
      const selected = id === this.selectedListingId;
      marker.getElement()?.classList.toggle('selected-marker', selected);
      marker.getElement()?.setAttribute('aria-pressed', String(selected));
      marker.setZIndexOffset(selected ? 1000 : 0);
      if (selected && pan && this.map && !this.map.getBounds().contains(marker.getLatLng())) {
        this.map.panTo(marker.getLatLng());
      }
    }
  }

  private hasValidCoordinates(listing: SearchListingItem): boolean {
    return Number.isFinite(listing.latitude)
      && Number.isFinite(listing.longitude)
      && listing.latitude >= -90
      && listing.latitude <= 90
      && listing.longitude >= -180
      && listing.longitude <= 180;
  }
}
