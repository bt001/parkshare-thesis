/** Persisted event addressed to the authenticated recipient. */
export interface NotificationItem {
  /** Notification identity. */
  id: number;
  /** Optional booking reference, absent for legacy events. */
  bookingId: number | null;
  /** Stable server event type. */
  type: string;
  /** Plain-text event description. */
  message: string;
  /** Persisted recipient read state. */
  isRead: boolean;
  /** UTC event timestamp. */
  createdAt: string;
}
/** Recipient-scoped page of notifications. */
export interface NotificationPage {
  /** Current one-based page. */
  page: number;
  /** Page capacity. */
  pageSize: number;
  /** Number matching the filter. */
  totalCount: number;
  /** Events on this page. */
  items: NotificationItem[];
}
