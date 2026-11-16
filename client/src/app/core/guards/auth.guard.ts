import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';

// Sends visitors without a session to the login page. An expired access token is fine:
// the HTTP interceptor refreshes it, and logs the user out if that fails.
export const authGuard: CanActivateFn = () => {
  if (localStorage.getItem('token') || localStorage.getItem('refreshToken')) {
    return true;
  }
  return inject(Router).createUrlTree(['/auth-form']);
};
