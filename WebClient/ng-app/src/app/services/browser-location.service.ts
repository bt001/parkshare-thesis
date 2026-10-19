import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';

/** Browser-only access to optional, one-shot device location. */
@Injectable({ providedIn: 'root' })
export class BrowserLocationService {
  /** Document provides a safe browser boundary during server rendering. */
  private readonly document = inject(DOCUMENT);

  /** Explains why location cannot be requested in this environment. */
  get unavailableReason(): string {
    const browser = this.document.defaultView;
    if (!browser) return 'Device location is not supported here.';
    if (!browser.isSecureContext) return 'Device location requires a secure HTTPS connection.';
    return browser.navigator.geolocation ? '' : 'Device location is not supported by this browser.';
  }

  /** Requests a fresh position only when explicitly invoked by the user. */
  locate(success: PositionCallback, failure: PositionErrorCallback): void {
    this.document.defaultView!.navigator.geolocation.getCurrentPosition(success, failure,
      { enableHighAccuracy: false, maximumAge: 0, timeout: 10_000 });
  }
}
