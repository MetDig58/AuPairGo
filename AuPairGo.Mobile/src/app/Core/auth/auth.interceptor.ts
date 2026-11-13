import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const isAuthEndpoint = /\/api\/Auth\/(login|register)/i.test(req.url);

  // Note: If you get a TS error here about '_token', just use a getter
  // in AuthService, but usually this works fine.
  const token = (auth as any)._token();

  const authReq = !isAuthEndpoint && token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq);
};
