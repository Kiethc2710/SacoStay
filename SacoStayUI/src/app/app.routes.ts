import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/auth/auth-page/auth-page.component').then(m => m.AuthPageComponent) },
  { path: 'register', loadComponent: () => import('./features/auth/auth-page/auth-page.component').then(m => m.AuthPageComponent) },
  { path: '', loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent), canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
