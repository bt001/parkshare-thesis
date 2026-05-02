import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap, throwError } from 'rxjs';

import { AuthTokenStorageService } from '../../services/auth-token-storage.service';
import { AuthSession, LoginRequest, LoginResult, RefreshTokenRequest } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(AuthTokenStorageService);
  private readonly sessionSubject = new BehaviorSubject<AuthSession | null>(this.tokenStorage.getSession());

  readonly session$ = this.sessionSubject.asObservable();

  get session(): AuthSession | null {
    return this.sessionSubject.value;
  }

  get isAuthenticated(): boolean {
    return this.hasUsableAccessToken(this.session);
  }

  get roles(): string[] {
    return this.session?.role ? [this.session.role] : [];
  }

  hasRole(role: string): boolean {
    return this.roles.includes(role);
  }

  login(request: LoginRequest): Observable<AuthSession> {
    return this.http.post<LoginResult>('api/auth/login', request).pipe(
      tap((session) => this.setSession(session))
    );
  }

  refresh(): Observable<AuthSession> {
    const refreshToken = this.tokenStorage.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token is available.'));
    }

    const request: RefreshTokenRequest = { refreshToken };
    return this.http.post<LoginResult>('api/auth/refresh', request).pipe(
      tap((session) => this.setSession(session))
    );
  }

  logout(): void {
    this.tokenStorage.clear();
    this.sessionSubject.next(null);
  }

  private setSession(session: AuthSession): void {
    this.tokenStorage.setSession(session);
    this.sessionSubject.next(session);
  }

  private hasUsableAccessToken(session: AuthSession | null): boolean {
    if (!session?.accessToken || !session.accessTokenExpiresAt) {
      return false;
    }

    return Date.parse(session.accessTokenExpiresAt) > Date.now();
  }
}
