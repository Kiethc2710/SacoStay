import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, of } from 'rxjs';
import { environment } from '../../environments/environment';
import type { LoginRequest, LoginResponse, UserProfile, RegisterRequest, RegisterResponse } from '../models/auth.models';

const TOKEN_KEY = 'saco_stay_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = environment.apiUrl;

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  get isLoggedIn(): boolean {
    return !!this.token;
  }

  login(body: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, body).pipe(
      tap((res) => {
        if (res?.token) {
          localStorage.setItem(TOKEN_KEY, res.token);
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.router.navigate(['/login']);
  }

  register(body: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.apiUrl}/auth/register`, body);
  }

  getProfile(): Observable<UserProfile | null> {
    return this.http.get<UserProfile>(`${this.apiUrl}/auth/profile`).pipe(
      catchError(() => of(null))
    );
  }
}
