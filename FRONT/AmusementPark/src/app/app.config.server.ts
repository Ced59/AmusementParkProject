import { HttpBackend } from '@angular/common/http';
import { TranslateLoader } from '@ngx-translate/core';
import { ApplicationConfig, mergeApplicationConfig } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';

import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { ServerApiBaseUrlBackend } from '@core/http/backends/server-api-base-url.backend';
import { ServerTranslateLoader } from '@core/i18n/server-translate.loader';

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(withRoutes(serverRoutes)),
    {
      provide: TranslateLoader,
      useClass: ServerTranslateLoader
    },
    {
      // Réécrit les URLs API vers l'origine interne au niveau transport (après le
      // transfer-cache d'Angular), pour garantir une clé de cache identique
      // SSR/navigateur et la réutilisation des données à l'hydratation.
      provide: HttpBackend,
      useClass: ServerApiBaseUrlBackend
    }
  ]
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
