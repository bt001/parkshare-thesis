import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ChatMessage, ChatPage } from './chat.models';

/** Participant-scoped conversation API. */
@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  /** Retrieves a bounded conversation page. */
  get(bookingId: number, page: number) {
    const params = new HttpParams().set('page', page).set('pageSize', 50);
    return this.http.get<ChatPage>(`api/booking/${bookingId}/messages`, { params });
  }
  /** Sends once; sender and time are assigned by the server. */
  send(bookingId: number, messageText: string) {
    return this.http.post<ChatMessage>(`api/booking/${bookingId}/messages`, { messageText });
  }
}
