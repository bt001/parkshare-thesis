import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BehaviorSubject, of, Subject, throwError } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { AuthSession } from '../api/auth/auth.models';
import { ListingPhotoService } from '../api/listings/listing-photo.service';
import { ListingPhoto } from '../api/listings/listing.models';
import { ListingPhotosComponent } from './listing-photos.component';

/** Photo manager reconciliation, lifecycle and gallery regressions. */
describe('ListingPhotosComponent', () => {
  let fixture: ComponentFixture<ListingPhotosComponent>;
  let component: ListingPhotosComponent;
  let sessions: BehaviorSubject<AuthSession | null>;
  let api: jasmine.SpyObj<ListingPhotoService>;
  const photo: ListingPhoto = { id: 4, url: 'api/listing/7/photos/4/content', uploadedAt: '2026-10-12T12:00:00Z' };
  const file = new File(['image'], 'parking.png', { type: 'image/png' });
  beforeEach(async () => {
    sessions = new BehaviorSubject<AuthSession | null>({ userId: 1 } as AuthSession);
    api = jasmine.createSpyObj<ListingPhotoService>('photos', ['get', 'upload', 'remove']);
    api.get.and.returnValue(of({ items: [] }));
    api.upload.and.returnValue(of(photo)); api.remove.and.returnValue(of({ id: 4 }));
    await TestBed.configureTestingModule({ imports: [ListingPhotosComponent], providers: [
      { provide: ListingPhotoService, useValue: api }, { provide: AuthService, useValue: { session$: sessions } }
    ] }).compileComponents();
    fixture = TestBed.createComponent(ListingPhotosComponent); component = fixture.componentInstance;
    fixture.componentRef.setInput('listingId', 7); fixture.componentRef.setInput('editable', true); fixture.detectChanges();
  });
  it('blocks mutations while metadata is unavailable and supports retry', () => {
    api.get.and.returnValue(throwError(() => new Error('offline'))); component.load();
    component.selected = file; component.upload(); expect(api.upload).not.toHaveBeenCalled();
    expect(component.error).toContain('Retry');
    api.get.and.returnValue(of({ items: [] })); component.load(); expect(component.ready).toBeTrue();
  });
  it('reconciles an uncertain upload and requires explicit file reselection', () => {
    const refresh = new Subject<{ items: ListingPhoto[] }>();
    api.upload.and.returnValue(throwError(() => new Error('lost response'))); api.get.and.returnValue(refresh);
    component.selected = file; component.upload();
    expect(component.ready).toBeFalse(); expect(component.selected).toBeUndefined();
    component.selected = file; component.upload(); expect(api.upload).toHaveBeenCalledTimes(1);
    refresh.next({ items: [photo] }); expect(component.photos).toEqual([photo]); expect(component.ready).toBeTrue();
  });
  it('clears photos and cancels pending upload on account change', () => {
    const pending = new Subject<ListingPhoto>(); api.upload.and.returnValue(pending);
    component.selected = file; component.upload(); sessions.next({ userId: 2 } as AuthSession);
    expect(pending.observed).toBeFalse(); expect(component.sessionChanged).toBeTrue(); expect(component.selected).toBeUndefined();
    pending.next(photo); expect(component.photos).toEqual([]); expect(component.ready).toBeFalse();
  });
  it('retains work during same-account token refresh', () => {
    component.selected = file; sessions.next({ userId: 1, accessToken: 'new' } as AuthSession);
    expect(component.selected).toBe(file); expect(component.sessionChanged).toBeFalse();
  });
  it('cancels work and resets file selection when the listing changes or the view is destroyed', () => {
    const pending = new Subject<ListingPhoto>(); api.upload.and.returnValue(pending);
    component.selected = file; component.upload(); fixture.componentRef.setInput('listingId', 8); fixture.detectChanges();
    expect(pending.observed).toBeFalse(); expect(component.selected).toBeUndefined(); expect(api.get).toHaveBeenCalledWith(8);
    const read = new Subject<{ items: ListingPhoto[] }>(); api.get.and.returnValue(read); component.load(); fixture.destroy();
    expect(read.observed).toBeFalse();
  });
  it('refreshes after failed deletion without discarding saved photos', () => {
    api.get.and.returnValue(of({ items: [photo] })); component.load();
    api.remove.and.returnValue(throwError(() => new Error('offline'))); component.remove(photo);
    expect(component.photos).toEqual([photo]); expect(component.message).toContain('could not be confirmed');
  });
  it('rejects unsupported and oversized local files and enforces the count limit', () => {
    component.select({ target: { files: [new File(['x'], 'x.svg', { type: 'image/svg+xml' })], value: 'x' } } as unknown as Event);
    expect(component.selected).toBeUndefined(); expect(component.message).toContain('JPEG');
    component.select({ target: { files: [new File([new Uint8Array(5 * 1024 * 1024 + 1)], 'x.png', { type: 'image/png' })], value: 'x' } } as unknown as Event);
    expect(component.selected).toBeUndefined();
    component.photos = Array.from({ length: 8 }, (_, i) => ({ ...photo, id: i })); component.selected = file; component.upload();
    expect(api.upload).not.toHaveBeenCalled();
  });
  it('renders public images, empty and broken states without owner controls', () => {
    fixture.componentRef.setInput('editable', false); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No photos yet.');
    api.get.and.returnValue(of({ items: [photo] })); component.load(); fixture.detectChanges();
    const image = fixture.nativeElement.querySelector('img') as HTMLImageElement;
    expect(image.src).toContain('/api/listing/7/photos/4/content'); expect(image.loading).toBe('lazy');
    image.dispatchEvent(new Event('error')); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Photo unavailable'); expect(fixture.nativeElement.querySelector('input')).toBeNull();
  });
});
