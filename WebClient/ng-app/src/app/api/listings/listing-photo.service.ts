import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ANONYMOUS_API } from '../api-request';
import { ListingPhoto } from './listing.models';

/** Listing photo upload, removal and public metadata client. */
@Injectable({ providedIn: 'root' })
export class ListingPhotoService {
  private readonly http = inject(HttpClient);
  /** Reads photos without relying on browser authentication. */
  get(listingId: number): Observable<{ items: ListingPhoto[] }> {
    return this.http.get<{ items: ListingPhoto[] }>(`api/listing/${listingId}/photos`, { context: new HttpContext().set(ANONYMOUS_API, true) });
  }
  /** Uploads exactly one image; the browser supplies the multipart boundary. */
  upload(listingId: number, file: File): Observable<ListingPhoto> {
    const body = new FormData(); body.append('file', file);
    return this.http.post<ListingPhoto>(`api/listing/${listingId}/photos`, body);
  }
  /** Removes one saved photo from its parent listing. */
  remove(listingId: number, photoId: number): Observable<{ id: number }> {
    return this.http.delete<{ id: number }>(`api/listing/${listingId}/photos/${photoId}`);
  }
}
