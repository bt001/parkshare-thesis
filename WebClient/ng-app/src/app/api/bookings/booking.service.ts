import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { BookingResult, CreateBookingRequest } from './booking.models';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);

  create(request: CreateBookingRequest): Observable<BookingResult> {
    return this.http.post<BookingResult>('api/booking/bookingrequest', request);
  }
}
