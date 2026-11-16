import {HttpErrorResponse, HttpInterceptorFn, HttpRequest} from "@angular/common/http";
import {inject} from "@angular/core";
import {catchError, switchMap, throwError} from "rxjs";
import {AuthService} from "../services/auth.service";

// Sends the access token with every request. On a 401 it refreshes the token once and retries;
// if the refresh fails, AuthService logs the user out.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = localStorage.getItem('token');

  return next(token ? withToken(req, token) : req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !req.url.includes('/auth/refresh')) {
        return authService.refreshAccessToken().pipe(
          switchMap(newToken => next(withToken(req, newToken)))
        );
      }
      return throwError(() => error);
    })
  );
};

function withToken<T>(req: HttpRequest<T>, token: string): HttpRequest<T> {
  return req.clone({setHeaders: {Authorization: `Bearer ${token}`}});
}
