export interface SearchListingsRequest {
  /** All selected amenities must match. */ amenityIds?: number[];
  /** Inclusive average rating. */ minRating?: number;
  /** Stable server ordering. */ sortBy?: 'newest' | 'priceAsc' | 'priceDesc' | 'ratingDesc';
  /** Resolved place centre and great-circle radius in kilometres. */
  lat?: number;
  lng?: number;
  radiusKm?: number;
  /** Requested Romanian wall-clock period. */
  startDateTime?: string;
  endDateTime?: string;
  page: number;
  pageSize: number;
  onlyActive?: boolean;
  /** Matches listing titles containing this text. */
  titleContains?: string;
  /** Matches addresses containing this text. */
  addressContains?: string;
  /** Inclusive minimum hourly price in RON. */
  minPrice?: number;
  /** Inclusive maximum hourly price in RON. */
  maxPrice?: number;
  /** Restricts results to listings with an always-available schedule. */
  onlyAlwaysAvailable?: boolean;
}

export interface SearchListingItem {
  /** Public cover image endpoint. */ coverPhotoUrl?: string | null;
  /** Unrounded review average. */ averageRating?: number | null;
  /** Saved review count. */ ratingCount?: number;
  /** Estimated RON cost for the applied period. */ estimatedPeriodCost?: number | null;
  id: number;
  title: string;
  address?: string | null;
  latitude: number;
  longitude: number;
  pricePerHour: number;
  isActive: boolean;
  isAlwaysAvailable: boolean;
  createdAt: string;
}

export interface SearchListingsResult {
  page: number;
  pageSize: number;
  totalCount: number;
  items: SearchListingItem[];
}

/** A catalog amenity selected by a listing owner. */
export interface Amenity {
  /** Catalog identifier. */
  id: number;
  /** Display label. */
  name: string;
  /** Optional catalog explanation. */
  description?: string | null;
}

export interface ListingDetails {
  /** Saved amenities, ordered by name. */
  amenities: Amenity[];
  id: number;
  ownerId: number;
  title: string;
  description: string;
  address?: string | null;
  latitude: number;
  longitude: number;
  size?: string | null;
  pricePerHour: number;
  isActive: boolean;
  isAlwaysAvailable: boolean;
  createdAt: string;
}

export interface OwnerListingItem {
  id: number;
  title: string;
  address?: string | null;
  pricePerHour: number;
  latitude: number;
  longitude: number;
  isActive: boolean;
  createdAt: string;
}

export interface GetOwnerListingsResult {
  items: OwnerListingItem[];
}

export interface CreateListingRequest {
  /** Null/omitted selects none on create and preserves selections on update; empty clears. */
  amenityIds?: number[] | null;
  title: string;
  description: string;
  pricePerHour: number;
  latitude: number;
  longitude: number;
  address?: string | null;
  size?: string | null;
  isAlwaysAvailable: boolean;
}

export type UpdateListingRequest = CreateListingRequest;

export interface ListingMutationResult {
  listingId: number;
  isActive: boolean;
}

/** Saved public photo metadata. */
export interface ListingPhoto {
  /** Photo identity. */ id: number;
  /** API-relative image endpoint. */ url: string;
  /** UTC upload timestamp. */ uploadedAt: string;
}

/** Resolved place name and WGS84 coordinates. */
export interface SearchPlace {
  /** Provider display label. */ label: string;
  /** Centre latitude. */ latitude: number;
  /** Centre longitude. */ longitude: number;
}
