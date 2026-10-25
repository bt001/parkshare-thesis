import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import { AvailabilityWindow } from '../api/availability/availability.models';
import { AvailabilityService } from '../api/availability/availability.service';
import { ListingDetails } from '../api/listings/listing.models';
import { ListingService } from '../api/listings/listing.service';

interface AvailabilityForm {
  startDateTime: FormControl<string>; endDateTime: FormControl<string>;
  isBlocked: FormControl<boolean>; note: FormControl<string>;
}

@Component({
  selector: 'app-owner-availability',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './owner-availability.component.html',
  styleUrl: './owner-workspace.scss'
})
export class OwnerAvailabilityComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly listingService = inject(ListingService);
  private readonly availabilityService = inject(AvailabilityService);

  readonly form = new FormGroup<AvailabilityForm>({
    startDateTime: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    endDateTime: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    isBlocked: new FormControl(false, { nonNullable: true }),
    note: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] })
  });

  listing?: ListingDetails;
  windows: AvailabilityWindow[] = [];
  editingId?: number;
  confirmingDeleteId?: number;
  isLoading = false;
  isSubmitting = false;
  errorMessages: string[] = [];

  /** Listing identifier retained for retrieval retries. */
  listingId?: number;
  /** Retrieval feedback, separate from window mutation failures. */
  loadErrors: string[] = [];
  /** Window currently being deleted. */
  deletingId?: number;
  /** Prevents conflicting actions while data is changing or unavailable. */
  get isBusy(): boolean { return this.isLoading || this.isSubmitting || this.deletingId !== undefined || this.loadErrors.length > 0; }

  get invalidRange(): boolean {
    const value = this.form.getRawValue();
    return !!value.startDateTime && !!value.endDateTime && value.startDateTime >= value.endDateTime;
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isSafeInteger(id) || id <= 0) { this.loadErrors = ['This listing address is invalid.']; return; }
    this.listingId = id;
    this.load();
  }

  edit(window: AvailabilityWindow): void {
    if (this.isBusy) return;
    this.editingId = window.availabilityWindowId;
    this.confirmingDeleteId = undefined;
    this.form.setValue({
      startDateTime: this.toLocalValue(window.startDateTime), endDateTime: this.toLocalValue(window.endDateTime),
      isBlocked: window.isBlocked, note: window.note ?? ''
    });
    if (this.listing?.isAlwaysAvailable) this.form.controls.isBlocked.disable();
  }

  cancelEdit(): void {
    if (this.isBusy) return;
    this.editingId = undefined;
    this.resetForm();
  }

  submit(): void {
    if (!this.listing || this.form.invalid || this.invalidRange || this.isBusy) { this.form.markAllAsTouched(); return; }
    const value = this.form.getRawValue();
    const request = { ...value, note: value.note.trim() || null };
    this.isSubmitting = true;
    this.errorMessages = [];
    const operation = this.editingId
      ? this.availabilityService.update(this.editingId, request)
      : this.availabilityService.create({ listingId: this.listing.id, ...request });
    operation.pipe(finalize(() => (this.isSubmitting = false))).subscribe({
      next: () => { this.editingId = undefined; this.resetForm(); this.load(); },
      error: (error) => (this.errorMessages = this.getErrors(error, 'The availability window could not be saved.'))
    });
  }

  requestDelete(id: number): void { if (!this.isBusy) this.confirmingDeleteId = id; }
  cancelDelete(): void { if (!this.isBusy) this.confirmingDeleteId = undefined; }

  delete(id: number): void {
    if (this.isBusy || this.confirmingDeleteId !== id) return;
    this.deletingId = id;
    this.errorMessages = [];
    this.availabilityService.delete(id).pipe(finalize(() => (this.deletingId = undefined))).subscribe({
      next: () => { this.confirmingDeleteId = undefined; if (this.editingId === id) { this.editingId = undefined; this.resetForm(); } this.load(); },
      error: (error) => (this.errorMessages = this.getErrors(error, 'The availability window could not be deleted.'))
    });
  }

  /** Loads current windows; retries retain any existing form draft. */
  load(): void {
    if (!this.listingId || this.isLoading) return;
    const id = this.listingId;
    this.isLoading = true;
    this.loadErrors = [];
    forkJoin({ listing: this.listingService.getById(id), availability: this.availabilityService.getForOwner({ listingId: id }) })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: ({ listing, availability }) => { const initial = !this.listing; this.listing = listing; this.windows = availability.items; if (initial) this.resetForm(); },
        error: (error) => (this.loadErrors = this.getErrors(error, 'Availability could not be loaded.'))
      });
  }

  private resetForm(): void {
    this.form.reset({ startDateTime: '', endDateTime: '', isBlocked: this.listing?.isAlwaysAvailable ?? false, note: '' });
    if (this.listing?.isAlwaysAvailable) this.form.controls.isBlocked.disable(); else this.form.controls.isBlocked.enable();
  }

  private toLocalValue(value: string): string { return value.slice(0, 16); }
  private getErrors(error: unknown, fallback: string): string[] {
    if (error instanceof HttpErrorResponse) {
      if (Array.isArray(error.error)) {
        const messages = error.error.filter((message): message is string => typeof message === 'string' && !!message.trim());
        return messages.length ? messages : [fallback];
      }
      if (typeof error.error === 'string' && error.error.trim()) return [error.error];
    }
    return [fallback];
  }
}
