
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApiService } from '../../../core/services/auth-api.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {

  loginForm: FormGroup;
  errorMessage = '';
  isLoading = false;

  showPassword = false;
  currentYear = new Date().getFullYear();

  constructor(
    private fb: FormBuilder,
    private authApi: AuthApiService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      username: ['', Validators.required],
      password: ['', Validators.required]
    });
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  onLogin(): void {

    this.errorMessage = '';

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;

    this.authApi.login(this.loginForm.value).subscribe({

      next: (response) => {

        localStorage.setItem('token', response.token);
        localStorage.setItem('username', response.username);
        localStorage.setItem(
          'adminRole',
          response.adminrole.toString()
        );

        this.isLoading = false;

        if (response.adminrole === 1) {
          this.router.navigate(['/salesman']);
        }
        else if (response.adminrole === 2) {
          this.router.navigate(['/marketing-team']);
        }
        else {
          localStorage.clear();
          this.errorMessage = 'Invalid admin role.';
        }
      },

      error: (error) => {

        this.isLoading = false;

        this.errorMessage =
          error.error?.message ||
          'Invalid username or password.';
      }

    });
  }
}
