import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ANONYMOUS_API } from '../api-request';
import { ListingPhotoService } from './listing-photo.service';

describe('ListingPhotoService', () => {
  it('uses anonymous metadata reads and authenticated multipart mutations', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const api = TestBed.inject(ListingPhotoService); const http = TestBed.inject(HttpTestingController);
    api.get(7).subscribe(); const read = http.expectOne('api/listing/7/photos');
    expect(read.request.context.get(ANONYMOUS_API)).toBeTrue(); read.flush({ items: [] });
    const file = new File(['image'], 'parking.png', { type: 'image/png' }); api.upload(7, file).subscribe();
    const upload = http.expectOne('api/listing/7/photos'); expect(upload.request.method).toBe('POST');
    expect(upload.request.body.get('file')).toBe(file); expect(upload.request.headers.has('Content-Type')).toBeFalse();
    expect(upload.request.context.get(ANONYMOUS_API)).toBeFalse(); upload.flush({ id: 4 });
    api.remove(7, 4).subscribe(); const remove = http.expectOne('api/listing/7/photos/4'); expect(remove.request.method).toBe('DELETE'); remove.flush({ id: 4 });
    http.verify();
  });
});
