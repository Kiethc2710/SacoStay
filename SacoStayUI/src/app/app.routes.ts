import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/auth/auth.component').then(m => m.AuthComponent) },
  { path: 'register', loadComponent: () => import('./pages/auth/auth.component').then(m => m.AuthComponent) },
  { path: 'auth', loadComponent: () => import('./pages/auth/auth.component').then(m => m.AuthComponent) },
  { path: 'otp-verification', loadComponent: () => import('./pages/otp/otp-verification.component').then(m => m.OtpVerificationComponent) },
  { path: '', loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent), canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
