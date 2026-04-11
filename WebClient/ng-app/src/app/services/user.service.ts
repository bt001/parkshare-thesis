import { Injectable } from '@angular/core';

import { AuthService } from '../api/auth/auth.service';

@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(private readonly authService: AuthService) {}

  get roles(): string[] {
    return this.authService.roles;
  }

  hasRole(role: string): boolean {
    return this.authService.hasRole(role);
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated;
  }
}
