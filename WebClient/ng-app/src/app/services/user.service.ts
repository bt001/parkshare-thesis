import { Injectable } from '@angular/core';

import { AuthService } from '../api/auth/auth.service';
import { AuthSession } from '../api/auth/auth.models';

@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(private readonly authService: AuthService) {}

  get session(): AuthSession | null {
    return this.authService.session;
  }

  get roles(): string[] {
    return this.authService.roles;
  }

  hasRole(role: string): boolean {
    return this.authService.hasRole(role);
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated;
  }

  get displayName(): string {
    return this.session?.name || this.session?.userName || 'ParkShare user';
  }

  get email(): string {
    return this.session?.email || '';
  }

  get role(): string {
    return this.session?.role || '';
  }
}
