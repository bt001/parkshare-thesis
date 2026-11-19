import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Directive, ElementRef, Input, OnChanges, OnDestroy, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { Subscription, fromEvent } from 'rxjs';
import { NotificationStateService } from '../notifications/notification-state.service';

/** Acknowledges only rendered activity intersecting a focused, visible viewport. */
@Directive({ selector: '[appAcknowledgeVisible]', standalone: true })
export class AcknowledgeVisibleDirective implements OnInit, OnChanges, OnDestroy {
  private readonly state = inject(NotificationStateService, { optional: true });
  private readonly element = inject(ElementRef<HTMLElement>);
  private readonly document = inject(DOCUMENT);
  private readonly browser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly subscriptions = new Subscription();
  private observer?: IntersectionObserver;
  private timer?: ReturnType<typeof setTimeout>;
  /** Notification identities included in this rendered row. */
  @Input() appAcknowledgeVisible: readonly number[] | null | undefined = [];
  /** Conversation identity for an incoming chat message. */
  @Input() readBookingId = 0;
  /** Incoming message identity; zero for own messages. */
  @Input() readMessageId = 0;
  /** Observes visibility and focus without acknowledging hidden rows. */
  ngOnInit(): void {
    if (!this.browser || !this.state) return;
    this.observer = new IntersectionObserver(() => this.schedule());
    this.observer.observe(this.element.nativeElement);
    this.subscriptions.add(this.state.pulse?.subscribe(() => this.schedule()));
    if (this.document.defaultView) this.subscriptions.add(fromEvent(this.document.defaultView, 'focus').subscribe(() => this.schedule()));
    this.schedule();
  }
  /** Waits for Angular to render updated inputs before evaluating visibility. */
  ngOnChanges(): void { this.schedule(); }
  /** Coalesces post-render acknowledgement checks. */
  private schedule(): void {
    if (!this.browser) return;
    clearTimeout(this.timer);
    this.timer = setTimeout(() => {
      if (!this.state?.canAcknowledge) return;
      const rect = this.element.nativeElement.getBoundingClientRect();
      const view = this.document.defaultView;
      if (!view || rect.width <= 0 || rect.height <= 0 || rect.bottom <= 0 || rect.top >= view.innerHeight || rect.right <= 0 || rect.left >= view.innerWidth) return;
      this.state.acknowledgeNotifications(this.appAcknowledgeVisible ?? []);
      if (this.readBookingId && this.readMessageId) this.state.acknowledgeMessage(this.readBookingId, this.readMessageId);
    });
  }
  /** Stops observers and pending callbacks on navigation. */
  ngOnDestroy(): void { clearTimeout(this.timer); this.observer?.disconnect(); this.subscriptions.unsubscribe(); }
}
