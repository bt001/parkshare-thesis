import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class UserService {
  private _roles: string[] = ['user', 'admin']; // mock

  get roles(): string[] {
    return this._roles;
  }

  hasRole(role: string): boolean {
    return this._roles.includes(role);
  }

  // Add methods for login, logout, etc, in the future
}
