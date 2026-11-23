import type { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'home',
    loadComponent: () => import('./Shared/home/home.page').then((m) => m.HomePage),
  },
  {
    path: 'login',
    loadComponent: () => import('./Shared/login/login.page').then((m) => m.LoginPage),
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./Shared/forgot-password/forgot-password.page').then((m) => m.ForgotPasswordPage),
  },
  {
    path: '',
    redirectTo: 'home',
    pathMatch: 'full',
  },
  {
    path: 'register',
    loadComponent: () => import('./Shared/register/register.page').then((m) => m.RegisterPage),
  },
  {
    path: 'parent-dashboard',
    loadComponent: () => import('./Parent/parent-dashboard/parent-dashboard.page').then((m) => m.ParentDashboardPage),
  },
  {
    path: 'au-pair-dashboard',
    loadComponent: () => import('./au-pair-dashboard/au-pair-dashboard.page').then((m) => m.AuPairDashboardPage),
  },
];
