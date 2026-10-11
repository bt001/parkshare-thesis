import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ChatService } from './chat.service';

describe('ChatService', () => {
  let service: ChatService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ChatService); http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('requests a bounded page and submits text without caller identity or timestamp', () => {
    service.get(9, 2).subscribe();
    const read = http.expectOne('api/booking/9/messages?page=2&pageSize=50');
    expect(read.request.method).toBe('GET'); read.flush({ items: [] });
    service.send(9, 'Hello').subscribe();
    const send = http.expectOne('api/booking/9/messages');
    expect(send.request.method).toBe('POST');
    expect(send.request.body).toEqual({ messageText: 'Hello' }); send.flush({});
  });
});
