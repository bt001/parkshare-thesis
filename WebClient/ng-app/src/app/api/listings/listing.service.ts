import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CreateListingRequest,
  GetOwnerListingsResult,
  ListingDetails,
  ListingMutationResult,
  SearchListingsRequest,
  SearchListingsResult,
  UpdateListingRequest
} from './listing.models';

@Injectable({ providedIn: 'root' })
export class ListingService {
  private readonly http = inject(HttpClient);

  search(request: SearchListingsRequest): Observable<SearchListingsResult> {
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize);

    if (request.onlyActive !== undefined) {
      params = params.set('onlyActive', request.onlyActive);
    }

    return this.http.get<SearchListingsResult>('api/listing/search', { params });
  }

  getById(id: number): Observable<ListingDetails> {
    return this.http.get<ListingDetails>(`api/listing/${id}`);
  }

  getOwnerListings(): Observable<GetOwnerListingsResult> {
    return this.http.get<GetOwnerListingsResult>('api/listing/owner');
  }

  create(request: CreateListingRequest): Observable<ListingMutationResult> {
    return this.http.post<ListingMutationResult>('api/listing', request);
  }

  update(id: number, request: UpdateListingRequest): Observable<ListingMutationResult> {
    return this.http.put<ListingMutationResult>(`api/listing/${id}`, request);
  }

  activate(id: number): Observable<ListingMutationResult> {
    return this.http.patch<ListingMutationResult>('api/listing/activate', { id });
  }

  deactivate(id: number): Observable<ListingMutationResult> {
    return this.http.patch<ListingMutationResult>('api/listing/deactivate', { id });
  }
}
