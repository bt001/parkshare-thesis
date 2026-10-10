import { isPlatformBrowser } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, PLATFORM_ID, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { safeReturnUrl, SessionNavigationService } from '../session-navigation.service';

import { AuthService, SessionChangedError } from '../../api/auth/auth.service';

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
  private get returnUrl(): string { return safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl')); }

  /** Cancels callbacks after leaving this screen. */
  private readonly destroyRef = inject(DestroyRef);
  /** Blocks form submission while saved credentials are being recovered. */
  isRecovering = false;
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

  /** Recovers browser credentials before allowing another authentication action. */
  constructor() {
    const navigation = inject(SessionNavigationService);
    if (!isPlatformBrowser(inject(PLATFORM_ID))) return;
    this.isRecovering = true;
    this.authService.ensureSession().pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.isRecovering = false)
    ).subscribe({
      next: outcome => {
        if (outcome === 'authenticated') void this.router.navigateByUrl(this.returnUrl);
        else if (outcome === 'unavailable') void this.router.navigateByUrl(navigation.destination(outcome, this.returnUrl));
      },
      error: () => { /* A newer authentication action owns navigation. */ }
    });
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting || this.isRecovering) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessages = [];
    this.isSubmitting = true;

    this.authService.login(this.form.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: () => {
          void this.router.navigateByUrl(this.returnUrl);
        },
        error: (error) => {
          if (!(error instanceof SessionChangedError)) this.errorMessages = this.getErrorMessages(error);
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
