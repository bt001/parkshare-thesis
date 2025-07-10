import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TestService {
  constructor(private http: HttpClient) {}

  ping(): Observable<string> {
    // Use a relative URL; the base URL is added by your interceptor!
    return this.http.get('api/test/ping', { responseType: 'text' });
  }
}
