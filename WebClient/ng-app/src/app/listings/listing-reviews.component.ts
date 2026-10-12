import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, Input, OnChanges, OnDestroy, inject } from '@angular/core';
import { Subscription, finalize } from 'rxjs';
import { RatingService } from '../api/ratings/rating.service';
import { ListingRatings } from '../api/ratings/rating.models';

/** Public reviews loaded independently from listing and booking content. */
@Component({
  selector: 'app-listing-reviews', standalone: true, imports: [DatePipe, DecimalPipe],
  template: `
    <section aria-labelledby="reviews-heading">
      <h2 id="reviews-heading">Reviews</h2>
      @if (loading) { <p role="status">Loading reviews…</p> }
      @else if (error) { <p role="alert">{{ error }}</p><button type="button" (click)="load(requestedPage)">Retry reviews</button> }
      @else if (result; as reviews) {
        @if (reviews.totalCount) {
          <p class="aggregate">{{ reviews.averageStars | number:'1.1-1' }} / 5 · {{ reviews.totalCount }} {{ reviews.totalCount === 1 ? 'review' : 'reviews' }}</p>
          @for (review of reviews.items; track review.id) {
            <article><strong>{{ review.stars }} / 5</strong><time [attr.datetime]="review.createdAt">{{ review.createdAt | date:'mediumDate' }}</time><p>{{ review.comment }}</p></article>
          }
          <nav aria-label="Review pages"><button type="button" [disabled]="reviews.page <= 1" (click)="load(reviews.page - 1)">Previous reviews</button><span>Page {{ reviews.page }}</span><button type="button" [disabled]="reviews.page * reviews.pageSize >= reviews.totalCount" (click)="load(reviews.page + 1)">Next reviews</button></nav>
        } @else { <p>No reviews yet.</p> }
      }
    </section>`,
  styles: [`section { margin-top: 2rem; padding: 1.5rem; background: white; border: 1px solid #e2e8f0; border-radius: 1rem; }
    article { border-top: 1px solid #e2e8f0; padding: 1rem 0; } time { margin-left: 1rem; color: #475569; }
    article p { white-space: pre-wrap; overflow-wrap: anywhere; } nav { display: flex; flex-wrap: wrap; align-items: center; gap: .75rem; }
    button { padding: .6rem .85rem; border: 1px solid #b9c8d5; border-radius: 999px; background: #fff; color: #16324f; font: inherit; font-weight: 700; cursor: pointer; }
    button:disabled { opacity: .55; cursor: not-allowed; } .aggregate { font-weight: 600; }`]
})
export class ListingReviewsComponent implements OnChanges, OnDestroy {
  /** Listing whose public reviews are displayed. */ @Input({ required: true }) listingId = 0;
  private readonly api = inject(RatingService);
  private request?: Subscription;
  /** Current saved result. */ result?: ListingRatings;
  /** Pending retrieval. */ loading = false;
  /** Retry feedback. */ error = '';
  /** Last requested page retained for retry. */ requestedPage = 1;
  /** Resets reviews whenever the listing changes. */
  ngOnChanges(): void { this.result = undefined; this.load(1); }
  /** Retrieves a page without interrupting the listing or booking form. */
  load(page: number): void {
    this.request?.unsubscribe(); this.requestedPage = page; this.loading = true; this.error = '';
    this.request = this.api.get(this.listingId, page).pipe(finalize(() => this.loading = false)).subscribe({
      next: result => this.result = result,
      error: () => this.error = 'Reviews could not be loaded. Please retry.'
    });
  }
  /** Cancels retrieval when leaving the page. */ ngOnDestroy(): void { this.request?.unsubscribe(); }
}
