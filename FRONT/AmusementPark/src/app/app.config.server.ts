import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { TranslateLoader } from '@ngx-translate/core';
import { ApplicationConfig, mergeApplicationConfig } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';

import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { ServerApiBaseUrlInterceptor } from '@core/http/interceptors/server-api-base-url.interceptor';
import { ServerTranslateLoader } from '@core/i18n/server-translate.loader';

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(withRoutes(serverRoutes)),
    {
      provide: TranslateLoader,
      useClass: ServerTranslateLoader
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ServerApiBaseUrlInterceptor,
      multi: true
    }
  ]
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
