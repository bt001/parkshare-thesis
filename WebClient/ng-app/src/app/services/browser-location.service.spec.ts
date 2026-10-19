import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { BrowserLocationService } from './browser-location.service';

describe('BrowserLocationService', () => {
  it('guards server, insecure and unsupported environments', () => {
    const document: any = { defaultView: null };
    TestBed.configureTestingModule({ providers: [{ provide: DOCUMENT, useValue: document }] });
    const service = TestBed.inject(BrowserLocationService);
    expect(service.unavailableReason).toContain('not supported');
    document.defaultView = { isSecureContext: false };
    expect(service.unavailableReason).toContain('HTTPS');
    document.defaultView = { isSecureContext: true, navigator: {} };
    expect(service.unavailableReason).toContain('not supported');
  });

  it('requests fresh bounded one-shot location with the supplied callbacks', () => {
    const getCurrentPosition = jasmine.createSpy('getCurrentPosition');
    TestBed.configureTestingModule({ providers: [{ provide: DOCUMENT, useValue: {
      defaultView: { isSecureContext: true, navigator: { geolocation: { getCurrentPosition } } }
    } }] });
    const service = TestBed.inject(BrowserLocationService);
    const success = jasmine.createSpy('success');
    const failure = jasmine.createSpy('failure');
    expect(service.unavailableReason).toBe('');
    expect(getCurrentPosition).not.toHaveBeenCalled();
    service.locate(success, failure);
    expect(getCurrentPosition).toHaveBeenCalledOnceWith(success, failure,
      { enableHighAccuracy: false, maximumAge: 0, timeout: 10000 });
  });
});
