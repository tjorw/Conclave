import { APP_INITIALIZER, ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideMarkdown } from 'ngx-markdown';

import { routes } from './app.routes';
import { ConventionContextService, ENVIRONMENT, tenantDevInterceptor, conventionInterceptor, authInterceptor, authSessionInterceptor } from 'shared';
import { environment } from '../environments/environment';
import { EditionService } from './services/edition.service';
import { BrandingService } from './services/branding.service';

function initPublicApp(
  conventionContext: ConventionContextService,
  editionService: EditionService,
  brandingService: BrandingService
): () => Promise<void> {
  return async () => {
    await conventionContext.load();
    await Promise.all([
      brandingService.load(),
      editionService.load(),
    ]);
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([tenantDevInterceptor, conventionInterceptor, authInterceptor, authSessionInterceptor])),
    { provide: ENVIRONMENT, useValue: environment },
    {
      provide: APP_INITIALIZER,
      useFactory: initPublicApp,
      deps: [ConventionContextService, EditionService, BrandingService],
      multi: true,
    },
    provideMarkdown(),
  ],
};
