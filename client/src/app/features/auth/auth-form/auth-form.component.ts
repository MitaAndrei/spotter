import {Component} from '@angular/core';
import {FormlyFieldConfig, FormlyModule} from '@ngx-formly/core';
import {FormGroup, ReactiveFormsModule} from '@angular/forms';
import {CommonModule} from '@angular/common';
import {AuthService} from "../../../core/services/auth.service";
import {SignUpCredentials} from "../../../core/models/sign-up-credentials.model";
import {LoginCredentials} from "../../../core/models/login-credentials.model";
import {Router} from "@angular/router";
import {FormlyMaterialModule} from "@ngx-formly/material";
import {MatButton} from "@angular/material/button";
import {MatIcon} from "@angular/material/icon";
import {SnackbarService} from "../../../core/services/snackbar.service";

type AuthMode = 'signup' | 'login';

@Component({
  selector: 'app-auth-form',
  standalone: true,
  imports: [
    FormlyModule,
    FormlyMaterialModule,
    ReactiveFormsModule,
    CommonModule,
    MatButton,
    MatIcon
  ],
  templateUrl: './auth-form.component.html',
  styleUrl: './auth-form.component.scss'
})
export class AuthFormComponent {

  constructor(public authService: AuthService,
              public router: Router,
              private snackbarService: SnackbarService,) {
  }

  mode: AuthMode = 'signup';
  form = new FormGroup({});
  model: {username?: string, password?: string, passwordConfirm?: string} = {};
  errorMessage: string = "";

  private readonly FIELDS_CONFIG = {
    signup: [{
      validators: {
        validation: [
          { name: 'fieldMatch', options: { errorPath: 'passwordConfirm' } },
        ],
      },
      fieldGroup: [
        {
          key: 'username',
          type: 'input',
          props: {
            label: 'Username',
            placeholder: 'Username',
            required: true,
            minLength: 3,
          },
        },
        {
          key: 'password',
          type: 'input',
          props: {
            type: 'password',
            label: 'Password',
            placeholder: 'Must be at least 3 characters long',
            required: true,
            minLength: 3,
          },
        },
        {
          key: 'passwordConfirm',
          type: 'input',
          props: {
            type: 'password',
            label: 'Confirm Password',
            placeholder: 'Please re-enter your password',
            required: true,
          },
        },
      ],
    }]
    ,
    login: [{
      fieldGroup: [
        {
          key: 'username',
          type: 'input',
          props: {
            label: 'Username',
            placeholder: 'Username',
            required: true,
            minLength: 3,
          },
        },
        {
          key: 'password',
          type: 'input',
          props: {
            type: 'password',
            label: 'Password',
            placeholder: 'Must be at least 3 characters long',
            required: true,
            minLength: 3,
          },
        }]
    }]
  };

  get fields(): FormlyFieldConfig[] {
    return this.FIELDS_CONFIG[this.mode];
  }
  get isSignupMode(): boolean {
    return this.mode === 'signup';
  }

  onSubmit() {
    if (!this.form.valid) {
      alert("Form is not valid")
      return;
    }

    if(this.isSignupMode) {
      this.handleSignup();
    }
    else{
      this.handleLogin();
    }
  }

  private handleSignup(): void {
    const credentials: SignUpCredentials = {
      username: this.model.username ?? '',
      password: this.model.password ?? '',
    };

    this.authService.signUp(credentials).subscribe({
      next: () => {
      this.toggleMode();
      this.snackbarService.success("Signed up successfully");

    },
      error: response => {
        this.errorMessage = response.error?.message ?? response.error?.title ?? 'Something went wrong';
      }
    })
  }

  private handleLogin(): void {
    const credentials: LoginCredentials = {
      username: this.model.username ?? '',
      password: this.model.password ?? '',
    };

    this.authService.logIn(credentials).subscribe(
      {
      next: response => {
        this.storeTokens(response);
        this.loadCurrentUser();
      },
    error: response => {
      this.errorMessage = response.error?.message ?? response.error?.title ?? 'Something went wrong';
    }
    });
  }

  private storeTokens(response: { accessToken: string; refreshToken: string }): void {
    localStorage.setItem("token", response.accessToken);
    localStorage.setItem("refreshToken", response.refreshToken);
  }

  private loadCurrentUser(): void {
    this.authService.getCurrentUser().subscribe({
      next: user => {
        this.authService.currentUserSig.set(user);
        this.router.navigate(['/profile', user.username]);
      },
      error: () => {
        this.authService.currentUserSig.set(null);
      }
    });
  }

  toggleMode(): void {
    this.mode = this.isSignupMode ? 'login' : 'signup';
    this.model = {};
    this.form = new FormGroup({});
    this.form.markAsUntouched();
    this.form.markAsPristine();
    this.errorMessage = "";
  }
}
