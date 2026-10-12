import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { BookingResult, CreateBookingRequest, GetOwnerBookingsRequest, GetOwnerBookingsResult, GetRenterBookingsRequest, GetRenterBookingsResult } from './booking.models';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);

  create(request: CreateBookingRequest): Observable<BookingResult> {
    return this.http.post<BookingResult>('api/booking/bookingrequest', request);
  }

  getForOwner(request: GetOwnerBookingsRequest): Observable<GetOwnerBookingsResult> {
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize);
    if (request.listingId !== undefined) params = params.set('listingId', request.listingId);
    for (const status of request.statuses ?? []) params = params.append('statuses', status);
    return this.http.get<GetOwnerBookingsResult>('api/booking/owner', { params });
  }

  /** Retrieves history scoped by the API to the authenticated renter. */
  getForRenter(request: GetRenterBookingsRequest): Observable<GetRenterBookingsResult> {
    let params = new HttpParams().set('page', request.page).set('pageSize', request.pageSize);
    for (const status of request.statuses ?? []) params = params.append('statuses', status);
    return this.http.get<GetRenterBookingsResult>('api/booking/renter', { params });
  }

  /** Completes an ended booking as its owner. */
  complete(bookingId: number): Observable<BookingResult> {
    return this.http.patch<BookingResult>('api/booking/completebooking', { bookingId });
  }

  approve(bookingId: number): Observable<BookingResult> {
    return this.http.patch<BookingResult>('api/booking/approvebooking', { bookingId });
  }

  reject(bookingId: number): Observable<BookingResult> {
    return this.http.patch<BookingResult>('api/booking/rejectbooking', { bookingId });
  }
}
