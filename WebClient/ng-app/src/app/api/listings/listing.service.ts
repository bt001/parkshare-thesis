import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { ANONYMOUS_API } from '../api-request';
import { inject, Injectable } from '@angular/core';
import { forkJoin, map, Observable, of, switchMap } from 'rxjs';

import {
  SearchPlace,
  Amenity,
  CreateListingRequest,
  GetOwnerListingsResult,
  ListingDetails,
  ListingMutationResult,
  SearchListingsRequest,
  SearchListingItem,
  SearchListingsResult,
  UpdateListingRequest
} from './listing.models';

@Injectable({ providedIn: 'root' })
export class ListingService {
  private readonly http = inject(HttpClient);

  /** Resolves an explicitly submitted place through the authenticated server adapter. */
  searchPlaces(text: string): Observable<{ items: SearchPlace[] }> {
    return this.http.get<{ items: SearchPlace[] }>('api/place/search', { params: { text } });
  }

  search(request: SearchListingsRequest): Observable<SearchListingsResult> {
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize);

    if (request.onlyActive !== undefined) {
      params = params.set('onlyActive', request.onlyActive);
    }

    for (const key of ['south', 'north', 'west', 'east', 'minRating', 'sortBy', 'lat', 'lng', 'radiusKm', 'startDateTime', 'endDateTime', 'titleContains', 'addressContains', 'minPrice', 'maxPrice', 'onlyAlwaysAvailable'] as const) {
      const value = request[key];
      if (value !== undefined) {
        params = params.set(key, value);
      }
    }

    for (const id of [...new Set(request.amenityIds ?? [])]) params = params.append('amenityIds', id);
    return this.http.get<SearchListingsResult>('api/listing/search', { params, context: new HttpContext().set(ANONYMOUS_API, true) });
  }

  searchAll(request: Omit<SearchListingsRequest, 'page' | 'pageSize'>): Observable<SearchListingItem[]> {
    const pageSize = 100;

    return this.search({ ...request, page: 1, pageSize }).pipe(
      switchMap((firstPage) => {
        const pageCount = Math.ceil(firstPage.totalCount / pageSize);
        if (pageCount <= 1) {
          return of(this.deduplicate(firstPage.items));
        }

        const remainingPages = Array.from(
          { length: pageCount - 1 },
          (_, index) => this.search({ ...request, page: index + 2, pageSize })
        );

        return forkJoin(remainingPages).pipe(
          map((results) => this.deduplicate([
            ...firstPage.items,
            ...results.flatMap((result) => result.items)
          ]))
        );
      })
    );
  }

  /** Loads the public amenity catalog. */
  getAmenities(): Observable<{ items: Amenity[] }> {
    return this.http.get<{ items: Amenity[] }>('api/amenity', { context: new HttpContext().set(ANONYMOUS_API, true) });
  }

  getById(id: number): Observable<ListingDetails> {
    return this.http.get<ListingDetails>(`api/listing/${id}`, { context: new HttpContext().set(ANONYMOUS_API, true) });
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

  private deduplicate(listings: SearchListingItem[]): SearchListingItem[] {
    return [...new Map(listings.map((listing) => [listing.id, listing])).values()];
  }
}
