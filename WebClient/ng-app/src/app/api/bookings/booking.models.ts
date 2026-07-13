export interface CreateBookingRequest {
  listingId: number;
  startDateTime: string;
  endDateTime: string;
}

export interface BookingResult {
  bookingId: number;
  listingId: number;
  renterId: number;
  ownerId: number;
  startDateTime: string;
  endDateTime: string;
  status: number;
  statusText: string;
  createdAt: string;
}

export enum BookingStatus {
  Pending = 0,
  Confirmed = 1,
  Rejected = 2,
  Completed = 3
}

export interface GetOwnerBookingsRequest {
  listingId?: number;
  statuses?: BookingStatus[];
  page: number;
  pageSize: number;
}

export interface OwnerBookingItem {
  bookingId: number;
  listingId: number;
  listingTitle: string;
  renterId: number;
  startUtc: string;
  endUtc: string;
  status: BookingStatus;
  statusText: string;
  createdAt: string;
}

export interface GetOwnerBookingsResult {
  page: number;
  pageSize: number;
  totalCount: number;
  items: OwnerBookingItem[];
}
