import {environment} from "../../../environments/environment";
import { HttpClient } from '@angular/common/http';
import {Injectable, signal} from '@angular/core';
import {catchError, finalize, map, Observable, shareReplay, tap, throwError} from 'rxjs';
import {SignUpCredentials} from "../models/sign-up-credentials.model";
import {LoginCredentials} from "../models/login-credentials.model";
import {User} from "../models/user.model";
import {TokenResponse} from "../models/token-response.model";
import {Router} from "@angular/router";

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  constructor(
    private http: HttpClient,
    private router: Router ){ }

  baseApiUrl: string = `${environment.apiUrl}/api/`
  currentUserSig = signal<User | null | undefined>(undefined);
  private userRequest$?: Observable<User>;
  // The refresh in progress, shared by every request that got a 401 meanwhile.
  private refreshInFlight$: Observable<string> | null = null;

  signUp(credentials: SignUpCredentials): Observable<void>{
    return this.http.post<void>(this.baseApiUrl + 'auth/register', credentials);
  }

  logIn(credentials: LoginCredentials): Observable<TokenResponse>{
    return this.http.post<TokenResponse>(this.baseApiUrl + 'auth/login', credentials);
  }

  logOut(){
    // Revoke this device's refresh token on the server; the local logout happens either way.
    const refreshToken = localStorage.getItem('refreshToken');
    if (refreshToken) {
      this.http.post(this.baseApiUrl + 'auth/logout', {refreshToken}).subscribe({error: () => {}});
    }

    localStorage.clear();
    this.currentUserSig.set(null);
    this.userRequest$ = undefined;
    this.router.navigate(['/']);
  }

  private fetchCurrentUser(): Observable<User>{
    return this.http.get<User>(this.baseApiUrl + 'users/me');
  }

  getCurrentUser(): Observable<User> {
    if (!this.userRequest$) {
      this.userRequest$ = this.fetchCurrentUser().pipe(
        shareReplay(1),
        tap(user => this.currentUserSig.set(user))
      );
    }
    return this.userRequest$;
  }

  // Exchanges the refresh token for new tokens and emits the new access token.
  // Concurrent callers share one request; if it fails, the user is logged out.
  refreshAccessToken(): Observable<string> {
    if (!this.refreshInFlight$) {
      const refreshToken = localStorage.getItem('refreshToken');
      this.refreshInFlight$ = this.http.post<TokenResponse>(this.baseApiUrl + 'auth/refresh', {refreshToken}).pipe(
        map(tokens => {
          this.setTokens(tokens);
          return tokens.accessToken;
        }),
        catchError(err => {
          this.logOut();
          return throwError(() => err);
        }),
        finalize(() => this.refreshInFlight$ = null),
        shareReplay(1)
      );
    }
    return this.refreshInFlight$;
  }

  setTokens(tokens: TokenResponse){
    localStorage.setItem('refreshToken', tokens.refreshToken);
    localStorage.setItem('token', tokens.accessToken);
  }

}
