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
import type { LayerGroup, Map as LeafletMap } from 'leaflet';

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
  @Output() readonly listingSelected = new EventEmitter<SearchListingItem>();

  isTileUnavailable = false;
  private leaflet?: typeof import('leaflet/dist/leaflet-src.esm.js');
  private map?: LeafletMap;
  private markerLayer?: LayerGroup;
  private isDestroyed = false;

  ngAfterViewInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      void this.initializeMap();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['listings'] && this.map) {
      this.renderMarkers();
    }
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
    this.map?.remove();
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
    }).setView([45.9432, 24.9668], 6);

    leaflet.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    })
      .on('tileerror', () => (this.isTileUnavailable = true))
      .addTo(this.map);

    this.markerLayer = leaflet.layerGroup().addTo(this.map);
    this.renderMarkers();
  }

  private renderMarkers(): void {
    if (!this.leaflet || !this.map || !this.markerLayer) {
      return;
    }

    this.markerLayer.clearLayers();
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

      this.leaflet.marker(coordinate, {
        icon,
        title: listing.title,
        alt: `${listing.title}, ${price} RON per hour`,
        riseOnHover: true
      })
        .on('click', () => this.listingSelected.emit(listing))
        .addTo(this.markerLayer);
    }

    this.map.invalidateSize();
    if (coordinates.length === 1) {
      this.map.setView(coordinates[0], 15);
    } else if (coordinates.length > 1) {
      this.map.fitBounds(this.leaflet.latLngBounds(coordinates), {
        padding: [40, 40],
        maxZoom: 15
      });
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
