import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { NotificationPage } from './notification.models';

/** API operations scoped by the server to the current recipient. */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  /** Retrieves an inbox page. */
  get(page: number, unreadOnly: boolean) {
    const params = new HttpParams().set('page', page).set('pageSize', 20).set('unreadOnly', unreadOnly);
    return this.http.get<NotificationPage>('api/notification', { params });
  }
  /** Retrieves the unread count across the whole inbox. */
  unreadCount() { return this.http.get<{ count: number }>('api/notification/unread-count'); }
  /** Idempotently marks a recipient-owned event read. */
  markRead(id: number) { return this.http.patch<{ isRead: boolean }>(`api/notification/${id}/read`, {}); }
}
