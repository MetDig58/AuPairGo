import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  const isAuthEndpoint = /\/api\/Auth\/(login|register)/i.test(req.url);
  let token = null;
  try {
    token = (auth as any)._token ? (auth as any)._token() : null;
  } catch (e) {
    console.error('Interceptor could not read token', e);
  }

  const authReq = !isAuthEndpoint && token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq);
};
