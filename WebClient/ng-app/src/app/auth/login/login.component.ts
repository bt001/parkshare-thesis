import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../api/auth/auth.service';

interface LoginForm {
  email: FormControl<string>;
  password: FormControl<string>;
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  /** Route containing the destination supplied by the authentication guard. */
  private readonly route = inject(ActivatedRoute);

  /** Accepts local application paths only, falling back to the dashboard. */
  private get returnUrl(): string {
    const value = this.route.snapshot.queryParamMap.get('returnUrl');
    return value?.startsWith('/') && !value.startsWith('//') && !value.includes('\\')
      && !/^\/(login|register)(?:[/?#;]|$)/i.test(value) ? value : '/dashboard';
  }

  readonly form = new FormGroup<LoginForm>({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email]
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    })
  });

  errorMessages: string[] = [];
  isSubmitting = false;

  constructor() {
    if (this.authService.hasAuthenticatedSession()) {
      void this.router.navigateByUrl(this.returnUrl);
    }
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessages = [];
    this.isSubmitting = true;

    this.authService.login(this.form.getRawValue())
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: () => {
          void this.router.navigateByUrl(this.returnUrl);
        },
        error: (error) => {
          this.errorMessages = this.getErrorMessages(error);
        }
      });
  }

  hasError(controlName: keyof LoginForm, errorCode: string): boolean {
    const control = this.form.controls[controlName];
    return control.touched && control.hasError(errorCode);
  }

  private getErrorMessages(error: unknown): string[] {
    if (error instanceof HttpErrorResponse) {
      if (Array.isArray(error.error) && error.error.every((item) => typeof item === 'string')) {
        return error.error;
      }

      if (typeof error.error === 'string' && error.error.trim()) {
        return [error.error];
      }

      if (error.message) {
        return [error.message];
      }
    }

    if (error instanceof Error && error.message) {
      return [error.message];
    }

    return ['Login failed. Please check your credentials and try again.'];
  }
}
