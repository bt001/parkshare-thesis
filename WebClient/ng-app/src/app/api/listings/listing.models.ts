export interface SearchListingsRequest {
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

export interface ListingDetails {
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
