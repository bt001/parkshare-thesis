import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, finalize, of, switchMap } from 'rxjs';

import { AuthService } from '../api/auth/auth.service';
import { CreateListingRequest } from '../api/listings/listing.models';
import { ListingService } from '../api/listings/listing.service';

interface ListingForm {
  title: FormControl<string>; description: FormControl<string>; address: FormControl<string>;
  latitude: FormControl<number | null>; longitude: FormControl<number | null>; pricePerHour: FormControl<number | null>;
  size: FormControl<string>; isAlwaysAvailable: FormControl<boolean>;
}

@Component({
  selector: 'app-owner-listing-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './owner-listing-form.component.html',
  styleUrl: './owner-workspace.scss'
})
export class OwnerListingFormComponent implements OnInit {
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

  listingId?: number;
  isLoading = false;
  isSubmitting = false;
  errorMessages: string[] = [];

  get isEditing(): boolean { return this.listingId !== undefined; }

  ngOnInit(): void {
    const rawId = this.route.snapshot.paramMap.get('id');
    if (!rawId) return;
    const id = Number(rawId);
    if (!Number.isSafeInteger(id) || id <= 0) { this.errorMessages = ['This listing address is invalid.']; return; }
    this.listingId = id;
    this.isLoading = true;
    this.listingService.getById(id).pipe(finalize(() => (this.isLoading = false))).subscribe({
      next: (listing) => this.form.setValue({
        title: listing.title, description: listing.description, address: listing.address ?? '', latitude: listing.latitude,
        longitude: listing.longitude, pricePerHour: listing.pricePerHour, size: listing.size ?? '', isAlwaysAvailable: listing.isAlwaysAvailable
      }),
      error: (error) => (this.errorMessages = this.getErrors(error, 'The listing could not be loaded.'))
    });
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting) { this.form.markAllAsTouched(); return; }
    const value = this.form.getRawValue();
    const request: CreateListingRequest = {
      ...value, latitude: value.latitude!, longitude: value.longitude!, pricePerHour: value.pricePerHour!,
      address: value.address.trim() || null, size: value.size.trim() || null
    };
    this.isSubmitting = true;
    this.errorMessages = [];
    const mutation = this.listingId ? this.listingService.update(this.listingId, request) : this.listingService.create(request);
    mutation.pipe(
      switchMap(() => this.listingId ? of(null) : this.authService.refresh().pipe(catchError(() => of(null)))),
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
