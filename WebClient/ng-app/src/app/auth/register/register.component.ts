import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../api/auth/auth.service';

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

  readonly form = this.createForm();

  errorMessages: string[] = [];
  isSubmitting = false;

  constructor() {
    if (this.authService.hasAuthenticatedSession()) {
      void this.router.navigateByUrl('/dashboard');
    }
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessages = [];
    this.isSubmitting = true;

    this.authService.register(this.form.getRawValue())
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: () => {
          void this.router.navigateByUrl('/dashboard');
        },
        error: (error) => {
          this.errorMessages = this.getErrorMessages(error);
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
