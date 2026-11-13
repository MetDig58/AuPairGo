import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAppInitializer, inject } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import {
  RouteReuseStrategy,
  provideRouter,
  withComponentInputBinding,
  withPreloading,
  PreloadAllModules,
} from '@angular/router';
import { IonicRouteStrategy, provideIonicAngular } from '@ionic/angular';

import { authInterceptor } from './app/Core/auth/auth.interceptor';
import { AuthService } from './app/Core/auth/auth.service';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';

bootstrapApplication(AppComponent, {
  providers: [
    { provide: RouteReuseStrategy, useClass: IonicRouteStrategy },
    provideIonicAngular(),
    provideRouter(routes, withPreloading(PreloadAllModules), withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),

    provideAppInitializer(async () => {
      const authService = inject(AuthService);
      await authService.restoreSession();
    }),
  ],
});
