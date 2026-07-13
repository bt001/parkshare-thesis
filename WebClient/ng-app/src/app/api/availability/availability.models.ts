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
