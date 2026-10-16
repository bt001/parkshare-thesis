import { HttpErrorResponse } from '@angular/common/http';
import { Component, Input, OnChanges, OnDestroy, OnInit, inject } from '@angular/core';
import { Subscription, distinctUntilChanged, map, skip } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { resolveApiUrl } from '../api/api-request';
import { ListingPhoto } from '../api/listings/listing.models';
import { ListingPhotoService } from '../api/listings/listing-photo.service';

/** Independent owner photo manager and public listing gallery. */
@Component({
  selector: 'app-listing-photos', standalone: true,
  templateUrl: './listing-photos.component.html', styleUrl: './listing-photos.component.scss'
})
export class ListingPhotosComponent implements OnChanges, OnInit, OnDestroy {
  /** Parent listing identity. */ @Input({ required: true }) listingId = 0;
  /** Enables owner upload and removal controls. */ @Input() editable = false;
  private readonly api = inject(ListingPhotoService);
  private readonly auth = inject(AuthService);
  private read?: Subscription;
  private mutation?: Subscription;
  private session?: Subscription;
  /** Saved photos only; unsaved listing fields are independent. */ photos: ListingPhoto[] = [];
  /** Failed images with a readable fallback. */ broken = new Set<number>();
  /** File selected explicitly for the next upload. */ selected?: File;
  /** Whether fresh metadata has been obtained. */ ready = false;
  /** Pending metadata read. */ loading = false;
  /** Pending mutation feedback. */ busy = '';
  /** Retrieval error with retry. */ error = '';
  /** Operation or selection feedback. */ message = '';
  /** Prevents stale owner work after an account change. */ sessionChanged = false;
  /** Resolves image sources using the same configured origin as HTTP requests. */ readonly imageUrl = resolveApiUrl;

  /** Cancels previous listing work and loads the new photo collection. */
  ngOnChanges(): void {
    this.cancel(); this.photos = []; this.broken.clear(); this.selected = undefined; this.message = ''; this.load();
  }
  /** Invalidates pending work on logout or account replacement, but not token refresh. */
  ngOnInit(): void {
    this.session = this.auth.session$.pipe(map(s => s?.userId), distinctUntilChanged(), skip(1)).subscribe(() => {
      this.cancel(); this.photos = []; this.selected = undefined; this.broken.clear(); this.message = '';
      if (this.editable) { this.sessionChanged = true; this.error = 'Your account changed. Reopen this listing to manage photos.'; }
      else this.load();
    });
  }
  /** Refreshes saved state before allowing another mutation. */
  load(): void {
    if (this.sessionChanged || this.busy) return;
    this.read?.unsubscribe(); this.ready = false; this.loading = true; this.error = ''; this.broken.clear();
    this.read = this.api.get(this.listingId).subscribe({
      next: result => { this.photos = result.items; this.ready = true; this.loading = false; },
      error: () => { this.loading = false; this.error = 'Photos could not be loaded. Retry before making changes.'; }
    });
  }
  /** Validates local file bounds while leaving signature checks to the server. */
  select(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0]; input.value = ''; this.selected = undefined; this.message = '';
    if (!file) return;
    if (!['image/jpeg', 'image/png'].includes(file.type) || !file.size || file.size > 5 * 1024 * 1024) {
      this.message = 'Select a JPEG or PNG image up to 5 MiB.'; return;
    }
    this.selected = file;
  }
  /** Uploads once, then reconciles metadata even if the result is uncertain. */
  upload(): void {
    if (!this.editable || !this.ready || this.busy || !this.selected || this.photos.length >= 8 || this.sessionChanged) return;
    this.busy = 'Uploading photo…'; this.message = ''; this.ready = false;
    this.mutation = this.api.upload(this.listingId, this.selected).subscribe({
      next: () => this.reconcile('Photo uploaded.'),
      error: error => this.reconcile(this.failure(error, 'Upload could not be confirmed. Check the refreshed photos before selecting the file again.'))
    });
  }
  /** Removes a saved photo without submitting the listing form. */
  remove(photo: ListingPhoto): void {
    if (!this.editable || !this.ready || this.busy || this.sessionChanged) return;
    this.busy = 'Removing photo…'; this.message = ''; this.ready = false;
    this.mutation = this.api.remove(this.listingId, photo.id).subscribe({
      next: () => this.reconcile('Photo removed.'),
      error: error => this.reconcile(this.failure(error, 'Removal could not be confirmed. Check the refreshed photos before retrying.'))
    });
  }
  /** Re-reads persisted state; uncertain uploads are never automatically repeated. */
  private reconcile(message: string): void {
    this.busy = ''; this.selected = undefined; this.message = message; this.load();
  }
  /** Displays safe API validation messages when available. */
  private failure(error: unknown, fallback: string): string {
    return error instanceof HttpErrorResponse && Array.isArray(error.error) && error.error.every(value => typeof value === 'string')
      ? error.error.join(' ') : fallback;
  }
  /** Cancels work and clears mutation eligibility. */
  private cancel(): void {
    this.read?.unsubscribe(); this.mutation?.unsubscribe(); this.ready = false; this.loading = false; this.busy = '';
  }
  /** Releases subscriptions and selected file references on navigation. */
  ngOnDestroy(): void { this.cancel(); this.session?.unsubscribe(); this.selected = undefined; }
}
