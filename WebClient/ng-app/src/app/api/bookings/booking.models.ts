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
