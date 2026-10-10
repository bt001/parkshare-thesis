import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { ANONYMOUS_API } from '../api-request';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AvailabilityWindow,
  AvailabilityWindowRequest,
  CreateAvailabilityWindowRequest,
  DeleteAvailabilityWindowResult,
  GetAvailabilityWindowsResult,
  GetOwnerAvailabilityRequest
} from './availability.models';

@Injectable({ providedIn: 'root' })
export class AvailabilityService {
  private readonly http = inject(HttpClient);

  getForListing(listingId: number, startDateTime?: string): Observable<GetAvailabilityWindowsResult> {
    let params = new HttpParams();
    if (startDateTime) {
      params = params.set('startDateTime', startDateTime);
    }

    return this.http.get<GetAvailabilityWindowsResult>(`api/availability/listing/${listingId}`, { params, context: new HttpContext().set(ANONYMOUS_API, true) });
  }

  getForOwner(request: GetOwnerAvailabilityRequest = {}): Observable<GetAvailabilityWindowsResult> {
    let params = new HttpParams();
    if (request.listingId !== undefined) params = params.set('listingId', request.listingId);
    if (request.startDateTime) params = params.set('startDateTime', request.startDateTime);
    if (request.endDateTime) params = params.set('endDateTime', request.endDateTime);
    return this.http.get<GetAvailabilityWindowsResult>('api/availability/owner', { params });
  }

  create(request: CreateAvailabilityWindowRequest): Observable<AvailabilityWindow> {
    return this.http.post<AvailabilityWindow>('api/availability', request);
  }

  update(id: number, request: AvailabilityWindowRequest): Observable<AvailabilityWindow> {
    return this.http.put<AvailabilityWindow>(`api/availability/${id}`, request);
  }

  delete(id: number): Observable<DeleteAvailabilityWindowResult> {
    return this.http.delete<DeleteAvailabilityWindowResult>(`api/availability/${id}`);
  }
}
