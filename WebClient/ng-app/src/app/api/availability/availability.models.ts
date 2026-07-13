export interface AvailabilityWindow {
  availabilityWindowId: number;
  listingId: number;
  listingTitle: string;
  startDateTime: string;
  endDateTime: string;
  isBlocked: boolean;
  note?: string | null;
  createdAt: string;
}

export interface GetAvailabilityWindowsResult {
  listingId?: number | null;
  totalCount: number;
  items: AvailabilityWindow[];
}

export interface GetOwnerAvailabilityRequest {
  listingId?: number;
  startDateTime?: string;
  endDateTime?: string;
}

export interface AvailabilityWindowRequest {
  startDateTime: string;
  endDateTime: string;
  isBlocked: boolean;
  note?: string | null;
}

export interface CreateAvailabilityWindowRequest extends AvailabilityWindowRequest {
  listingId: number;
}

export interface DeleteAvailabilityWindowResult {
  availabilityWindowId: number;
}
