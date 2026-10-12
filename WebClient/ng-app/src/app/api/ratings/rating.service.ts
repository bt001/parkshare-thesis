import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ANONYMOUS_API } from '../api-request';
import { ListingRatings, Rating } from './rating.models';

/** Persists renter reviews and retrieves anonymous listing reviews. */
@Injectable({ providedIn: 'root' })
export class RatingService {
  private readonly http = inject(HttpClient);
  /** Creates or replaces the current renter's review. */
  save(bookingId: number, stars: number, comment: string): Observable<Rating> {
    return this.http.put<Rating>(`api/booking/${bookingId}/rating`, { stars, comment });
  }
  /** Retrieves twenty reviews and the complete listing aggregate. */
  get(listingId: number, page = 1): Observable<ListingRatings> {
    return this.http.get<ListingRatings>(`api/listing/${listingId}/ratings`, {
      params: { page, pageSize: 20 }, context: new HttpContext().set(ANONYMOUS_API, true)
    });
  }
}
