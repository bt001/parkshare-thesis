import { ListingPhotosComponent } from '../listings/listing-photos.component';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, finalize, of, switchMap, throwError } from 'rxjs';

import { AuthService, SessionChangedError } from '../api/auth/auth.service';
import { Amenity, CreateListingRequest } from '../api/listings/listing.models';
import { ListingService } from '../api/listings/listing.service';

interface ListingForm {
  title: FormControl<string>; description: FormControl<string>; address: FormControl<string>;
  latitude: FormControl<number | null>; longitude: FormControl<number | null>; pricePerHour: FormControl<number | null>;
  size: FormControl<string>; isAlwaysAvailable: FormControl<boolean>;
}

@Component({
  selector: 'app-owner-listing-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ListingPhotosComponent],
  templateUrl: './owner-listing-form.component.html',
  styleUrl: './owner-listing-form.component.scss'
})
export class OwnerListingFormComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly listingService = inject(ListingService);
  private readonly authService = inject(AuthService);

  readonly form = new FormGroup<ListingForm>({
    title: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(120)] }),
    description: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(2000)] }),
    address: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(200)] }),
    latitude: new FormControl<number | null>(null, [Validators.required, Validators.min(-90), Validators.max(90)]),
    longitude: new FormControl<number | null>(null, [Validators.required, Validators.min(-180), Validators.max(180)]),
    pricePerHour: new FormControl<number | null>(null, [Validators.required, Validators.min(0.01)]),
    size: new FormControl('', { nonNullable: true }),
    isAlwaysAvailable: new FormControl(true, { nonNullable: true })
  });

  /** Catalog available for selection. */
  amenities: Amenity[] = [];
  /** Current draft selections, retained after failed saves. */
  selectedAmenityIds: number[] = [];
  /** Whether the catalog is loading. */
  isCatalogLoading = false;
  /** Whether catalog retrieval succeeded. */
  isCatalogReady = false;
  /** Catalog-specific retry message. */
  catalogError = '';

  /** Loads or retries the catalog without resetting the listing draft. */
  loadAmenities(): void {
    if (this.isCatalogLoading) return;
    this.isCatalogLoading = true;
    this.isCatalogReady = false;
    this.catalogError = '';
    this.listingService.getAmenities().pipe(takeUntilDestroyed(this.destroyRef), finalize(() => (this.isCatalogLoading = false))).subscribe({
      next: result => { this.amenities = result.items; this.isCatalogReady = true; },
      error: () => (this.catalogError = 'Amenities could not be loaded. Please retry before saving.')
    });
  }

  /** Updates the selected amenity set from a checkbox. */
  toggleAmenity(id: number, checked: boolean): void {
    this.selectedAmenityIds = checked ? [...new Set([...this.selectedAmenityIds, id])] : this.selectedAmenityIds.filter(value => value !== id);
  }

  listingId?: number;
  isLoading = false;
  isSubmitting = false;
  errorMessages: string[] = [];

  get isEditing(): boolean { return this.listingId !== undefined; }

  ngOnInit(): void {
    this.loadAmenities();
    const rawId = this.route.snapshot.paramMap.get('id');
    if (!rawId) return;
    const id = Number(rawId);
    if (!Number.isSafeInteger(id) || id <= 0) { this.errorMessages = ['This listing address is invalid.']; return; }
    this.listingId = id;
    this.isLoading = true;
    this.listingService.getById(id).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => (this.isLoading = false))).subscribe({
      next: (listing) => {
        this.selectedAmenityIds = listing.amenities.map(amenity => amenity.id);
        this.form.setValue({
        title: listing.title, description: listing.description, address: listing.address ?? '', latitude: listing.latitude,
        longitude: listing.longitude, pricePerHour: listing.pricePerHour, size: listing.size ?? '', isAlwaysAvailable: listing.isAlwaysAvailable
      });
      },
      error: (error) => (this.errorMessages = this.getErrors(error, 'The listing could not be loaded.'))
    });
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting || this.isLoading || !this.isCatalogReady) { this.form.markAllAsTouched(); return; }
    const value = this.form.getRawValue();
    const request: CreateListingRequest = {
      ...value, amenityIds: [...this.selectedAmenityIds], latitude: value.latitude!, longitude: value.longitude!, pricePerHour: value.pricePerHour!,
      address: value.address.trim() || null, size: value.size.trim() || null
    };
    this.isSubmitting = true;
    this.errorMessages = [];
    const mutation = this.listingId ? this.listingService.update(this.listingId, request) : this.listingService.create(request);
    mutation.pipe(
      takeUntilDestroyed(this.destroyRef),
      switchMap(() => this.listingId ? of(null) : this.authService.refresh().pipe(catchError(error => error instanceof SessionChangedError ? throwError(() => error) : of(null)))),
      finalize(() => (this.isSubmitting = false))
    ).subscribe({
      next: () => void this.router.navigateByUrl('/owner/listings'),
      error: (error) => (this.errorMessages = this.getErrors(error, `The listing could not be ${this.isEditing ? 'updated' : 'created'}.`))
    });
  }

  private getErrors(error: unknown, fallback: string): string[] {
    if (error instanceof HttpErrorResponse) {
      if (Array.isArray(error.error)) return error.error;
      if (typeof error.error === 'string' && error.error.trim()) return [error.error];
      if (error.message) return [error.message];
    }
    return [fallback];
  }
}
