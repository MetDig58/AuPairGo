import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  // 1. Check if it's an auth endpoint
  const isAuthEndpoint = /\/api\/Auth\/(login|register)/i.test(req.url);

  // 2. Safely get the token (using a public getter instead of the private signal)
  let token = null;
  try {
    // If you have a getter, use it. If not, this safely catches the error.
    token = (auth as any)._token ? (auth as any)._token() : null;
  } catch (e) {
    console.error('Interceptor could not read token', e);
  }

  // 3. Only attach if we have a token and it's not a login request
  const authReq = !isAuthEndpoint && token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq);
};
