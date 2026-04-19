import { APP_INITIALIZER, ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { ENVIRONMENT, conventionInterceptor, authInterceptor, authSessionInterceptor } from 'shared';
import { environment } from '../environments/environment';
import { EditionService } from './services/edition.service';

function initEdition(svc: EditionService): () => Promise<void> {
  return () => svc.load();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([conventionInterceptor, authInterceptor, authSessionInterceptor])),
    { provide: ENVIRONMENT, useValue: environment },
    {
      provide: APP_INITIALIZER,
      useFactory: initEdition,
      deps: [EditionService],
      multi: true,
    },
  ],
};
