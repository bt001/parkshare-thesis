import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { GetAvailabilityWindowsResult } from './availability.models';

@Injectable({ providedIn: 'root' })
export class AvailabilityService {
  private readonly http = inject(HttpClient);

  getForListing(listingId: number, startDateTime?: string): Observable<GetAvailabilityWindowsResult> {
    let params = new HttpParams();
    if (startDateTime) {
      params = params.set('startDateTime', startDateTime);
    }

    return this.http.get<GetAvailabilityWindowsResult>(`api/availability/listing/${listingId}`, { params });
  }
}
