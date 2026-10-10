import { isPlatformBrowser } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, PLATFORM_ID, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { safeReturnUrl, SessionNavigationService } from '../session-navigation.service';

import { AuthService, SessionChangedError } from '../../api/auth/auth.service';

const profileFieldMaxLength = 100;

interface RegisterForm {
  firstName: FormControl<string>;
  lastName: FormControl<string>;
  userName: FormControl<string>;
  email: FormControl<string>;
  password: FormControl<string>;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  /** Cancels callbacks after leaving this screen. */
  private readonly destroyRef = inject(DestroyRef);
  /** Blocks form submission while saved credentials are being recovered. */
  isRecovering = false;
  readonly form = this.createForm();

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
        if (outcome === 'authenticated') void this.router.navigateByUrl('/dashboard');
        else if (outcome === 'unavailable') void this.router.navigateByUrl(navigation.destination(outcome, '/dashboard'));
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

    this.authService.register(this.form.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: () => {
          void this.router.navigateByUrl('/dashboard');
        },
        error: (error) => {
          if (!(error instanceof SessionChangedError)) this.errorMessages = this.getErrorMessages(error);
        }
      });
  }

  hasError(controlName: keyof RegisterForm, errorCode: string): boolean {
    const control = this.form.controls[controlName];
    return control.touched && control.hasError(errorCode);
  }

  private createForm(): FormGroup<RegisterForm> {
    return new FormGroup<RegisterForm>({
      firstName: new FormControl('', {
        nonNullable: true,
        validators: this.createProfileFieldValidators()
      }),
      lastName: new FormControl('', {
        nonNullable: true,
        validators: this.createProfileFieldValidators()
      }),
      userName: new FormControl('', {
        nonNullable: true,
        validators: this.createProfileFieldValidators()
      }),
      email: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email]
      }),
      password: new FormControl('', {
        nonNullable: true,
        validators: this.createPasswordValidators()
      })
    });
  }

  private createProfileFieldValidators(): ValidatorFn[] {
    return [Validators.required, Validators.maxLength(profileFieldMaxLength)];
  }

  private createPasswordValidators(): ValidatorFn[] {
    return [Validators.required];
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

    return ['Registration failed. Please review the form and try again.'];
  }
}
