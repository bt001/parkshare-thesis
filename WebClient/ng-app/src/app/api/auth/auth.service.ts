import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, inject, Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subject, catchError, defer, finalize, map, of, race, shareReplay, switchMap, take, tap, throwError, timeout } from 'rxjs';
import { AuthTokenStorageService, isAuthSession, sessionStorageKey } from '../../services/auth-token-storage.service';
import { AuthSession, LoginRequest, LoginResult, RegisterRequest } from './auth.models';

/** Result of checking or recovering browser authentication. */
export type SessionOutcome = 'authenticated' | 'signed-out' | 'unavailable';
/** Signals that a newer authentication action superseded pending work. */
export class SessionChangedError extends Error {
  /** Creates a cancellation error without exposing credentials. */
  constructor() { super('The session changed. Please try again.'); }
}

/** Coordinates authentication and a single refresh request per browser tab. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  /** API transport, resolved before any request starts. */
  private readonly http = inject(HttpClient);
  /** Browser-safe session persistence. */
  private readonly tokenStorage = inject(AuthTokenStorageService);
  /** Current credentials, including recoverable expired-access sessions. */
  private readonly sessionSubject = new BehaviorSubject<AuthSession | null>(this.restoreSession());
  /** Cancels authentication work when its identity changes. */
  private readonly invalidated = new Subject<void>();
  /** Monotonic identity generation; refresh does not change identity. */
  private generation = 0;
  /** Successful credential replacements, including identical same-second JWTs. */
  private credentialRevision = 0;
  /** Shared refresh currently in progress. */
  private inFlight?: Observable<AuthSession>;
  /** Shared explicit refresh queued behind an older refresh. */
  private queuedRefresh?: Observable<AuthSession>;
  /** A challenged access token remains unusable until recovery succeeds. */
  private recoveryRequired = false;
  /** External changes used to discard another account's rendered content. */
  readonly externalChanges = new Subject<'logout' | 'account'>();
  /** Observable session for existing consumers. */
  readonly session$ = this.sessionSubject.asObservable();

  /** Subscribes to cross-tab changes only in a browser. */
  constructor() {
    if (this.tokenStorage.browser) {
      const listener = (event: StorageEvent) => {
        if (event.key === sessionStorageKey || event.key === null) this.synchronizeStorage();
      };
      window.addEventListener('storage', listener);
      inject(DestroyRef).onDestroy(() => window.removeEventListener('storage', listener));
    }
  }

  /** Current session; usable authentication must be checked separately. */
  get session(): AuthSession | null { return this.sessionSubject.value; }
  /** Identity generation captured by requests to prevent cross-account replay. */
  get sessionGeneration(): number { return this.generation; }
  /** Revision used to reuse refreshes for delayed authentication challenges. */
  get sessionRevision(): number { return this.credentialRevision; }
  /** Whether access credentials are currently usable. */
  get isAuthenticated(): boolean { return this.hasAuthenticatedSession(); }
  /** Assigned roles, retaining compatibility with older role strings. */
  get roles(): string[] {
    return this.session?.roles?.length ? this.session.roles
      : this.session?.role.split(',').map(role => role.trim()).filter(Boolean) ?? [];
  }
  /** Checks an assigned role without blocking owner onboarding. */
  hasRole(role: string): boolean { return this.roles.includes(role); }
  /** Checks access expiry without discarding refresh credentials. */
  hasAuthenticatedSession(): boolean {
    return !this.recoveryRequired && !!this.session && Date.parse(this.session.accessTokenExpiresAt) > Date.now();
  }

  /** Starts a new login identity and cancels older authentication work. */
  login(request: LoginRequest): Observable<AuthSession> { return this.authenticate('login', request); }
  /** Starts registration and adopts the returned authenticated session. */
  register(request: RegisterRequest): Observable<AuthSession> { return this.authenticate('register', request); }

  /** Returns current authentication or performs one recoverable refresh. */
  ensureSession(): Observable<SessionOutcome> {
    return defer(() => {
      this.synchronizeStorage();
      if (this.hasAuthenticatedSession()) return of<SessionOutcome>('authenticated');
      if (!this.session) return of<SessionOutcome>('signed-out');
      return this.recover().pipe(map((): SessionOutcome => 'authenticated'), catchError(error => {
        if (error instanceof SessionChangedError) return throwError(() => error);
        return of<SessionOutcome>(this.session ? 'unavailable' : 'signed-out');
      }));
    });
  }

  /** Shares a refresh for expiry or an authentication challenge. */
  recover(requiresRecovery = false): Observable<AuthSession> {
    return defer(() => {
      this.synchronizeStorage();
      if (requiresRecovery) this.recoveryRequired = true;
      if (this.inFlight) return this.inFlight;
      const session = this.session;
      if (!session || Date.parse(session.refreshTokenExpiresAt) <= Date.now()) {
        this.logout();
        return throwError(() => new Error('Your session has expired. Please sign in.'));
      }
      const generation = this.generation;
      let shared: Observable<AuthSession>;
      shared = this.withIdentity(this.http.post<LoginResult>('api/auth/refresh', { refreshToken: session.refreshToken }).pipe(
        timeout(15000),
        tap(result => {
          this.assertIdentity(generation);
          if (!isAuthSession(result) || result.userId !== session.userId || Date.parse(result.accessTokenExpiresAt) <= Date.now()) {
            throw new Error('The session response could not be validated.');
          }
          this.setSession(result);
        }),
        catchError(error => {
          this.assertIdentity(generation);
          if (error instanceof HttpErrorResponse && error.status === 400
            && Array.isArray(error.error) && error.error.includes('Invalid refresh token.')) {
            this.generation++;
            this.tokenStorage.clear();
            this.sessionSubject.next(null);
          }
          return throwError(() => error);
        })
      ), generation).pipe(
        finalize(() => { if (this.inFlight === shared) this.inFlight = undefined; }),
        shareReplay({ bufferSize: 1, refCount: false })
      );
      this.inFlight = shared;
      return shared;
    });
  }

  /** Refreshes roles after a committed mutation, waiting for older refresh work. */
  refresh(): Observable<AuthSession> {
    return defer(() => {
      if (!this.inFlight) return this.recover();
      if (this.queuedRefresh) return this.queuedRefresh;
      const generation = this.generation;
      const previous = this.inFlight;
      let queued: Observable<AuthSession>;
      queued = previous.pipe(
        catchError(error => error instanceof SessionChangedError ? throwError(() => error) : of(null)),
        switchMap(() => {
          this.assertIdentity(generation);
          if (this.inFlight === previous) this.inFlight = undefined;
          return this.recover();
        }),
        finalize(() => { if (this.queuedRefresh === queued) this.queuedRefresh = undefined; }),
        shareReplay({ bufferSize: 1, refCount: false })
      );
      this.queuedRefresh = queued;
      return queued;
    });
  }

  /** Invalidates pending requests and clears credentials. */
  logout(): void {
    this.invalidate();
    this.tokenStorage.clear();
    this.sessionSubject.next(null);
  }

  /** Ensures a request still belongs to its original authentication identity. */
  assertIdentity(generation: number): void {
    this.synchronizeStorage();
    if (generation !== this.generation) throw new SessionChangedError();
  }

  /** Reconciles storage before accepting results, including delayed storage events. */
  private synchronizeStorage(): void {
    const stored = this.tokenStorage.getSession();
    if (JSON.stringify(stored) === JSON.stringify(this.session)) return;
    const previousId = this.session?.userId;
    this.invalidate();
    const current = this.usableSession(stored);
    this.sessionSubject.next(current);
    if (!current) this.externalChanges.next('logout');
    else if (previousId !== current.userId) this.externalChanges.next('account');
  }

  /** Cancels obsolete authentication work and resets coordination. */
  private invalidate(): void {
    this.generation++;
    this.recoveryRequired = false;
    this.inFlight = undefined;
    this.queuedRefresh = undefined;
    this.invalidated.next();
  }

  /** Applies validation and identity checks to login and registration results. */
  private authenticate(path: string, request: LoginRequest | RegisterRequest): Observable<AuthSession> {
    return defer(() => {
      this.logout();
      const generation = this.generation;
      return this.withIdentity(this.http.post<LoginResult>('api/auth/' + path, request).pipe(tap(session => {
        this.assertIdentity(generation);
        if (!isAuthSession(session) || Date.parse(session.accessTokenExpiresAt) <= Date.now()) throw new Error('Invalid authentication response.');
        this.setSession(session);
      })), generation);
    });
  }

  /** Cancels transport when a newer login, logout, or external session wins. */
  private withIdentity(source: Observable<AuthSession>, generation: number): Observable<AuthSession> {
    return race(source, this.invalidated.pipe(take(1), switchMap(() => throwError(() => new SessionChangedError())))).pipe(
      tap(() => this.assertIdentity(generation))
    );
  }

  /** Saves a validated session before notifying consumers. */
  private setSession(session: AuthSession): void {
    this.credentialRevision++;
    this.recoveryRequired = false;
    this.tokenStorage.setSession(session);
    this.sessionSubject.next(session);
  }
  /** Restores credentials without discarding a usable refresh token. */
  private restoreSession(): AuthSession | null { return this.usableSession(this.tokenStorage.getSession()); }
  /** Drops sessions only when neither credential can be used. */
  private usableSession(session: AuthSession | null): AuthSession | null {
    if (session && (Date.parse(session.accessTokenExpiresAt) > Date.now() || Date.parse(session.refreshTokenExpiresAt) > Date.now())) return session;
    this.tokenStorage.clear();
    return null;
  }
}
