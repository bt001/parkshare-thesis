/** Persisted plain-text booking message. */
export interface ChatMessage {
  /** Message identity. */ id: number;
  /** Parent booking. */ bookingId: number;
  /** Server-derived sender. */ senderId: number;
  /** Plain text. */ messageText: string;
  /** UTC timestamp. */ sentAt: string;
}
/** Newest-first conversation page and participant identities. */
export interface ChatPage {
  /** One-based page. */ page: number;
  /** Page capacity. */ pageSize: number;
  /** Conversation size. */ totalCount: number;
  /** Owner identity. */ ownerId: number;
  /** Renter identity. */ renterId: number;
  /** Newest-first messages. */ items: ChatMessage[];
}
