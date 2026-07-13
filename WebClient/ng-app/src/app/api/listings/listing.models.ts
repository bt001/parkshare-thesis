export interface SearchListingsRequest {
  page: number;
  pageSize: number;
  onlyActive?: boolean;
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
