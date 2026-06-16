import { ApplicationConfig, importProvidersFrom, inject, provideAppInitializer } from '@angular/core';
import {
  provideHttpClient,
  withFetch,
  withInterceptorsFromDi,
  HTTP_INTERCEPTORS,
  HttpClient
} from '@angular/common/http';
import { provideClientHydration } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';

import { routes } from './app.routes';
import { initializeApp, HttpLoaderFactory } from './app.module';
import { TranslationService } from './services/translation.service';
import { AuthService } from './services/auth/auth.service';
import { LanguageInterceptor } from '@core/http/interceptors/language.interceptor';
import { AuthInterceptor } from '@core/http/interceptors/auth.interceptor';
import AmusementParkPreset from './config/primeng-preset';
import { DeploymentVersionService } from '@core/deployment/deployment-version.service';
import { MatomoPageViewTrackingService } from '@core/analytics/matomo-page-view-tracking.service';
import { MicrosoftClarityTrackingService } from '@core/analytics/microsoft-clarity-tracking.service';


export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(
      routes,
      withInMemoryScrolling({
        anchorScrolling: 'enabled',
        scrollPositionRestoration: 'enabled'
      })
    ),
    importProvidersFrom(
      TranslateModule.forRoot({
        loader: {
          provide: TranslateLoader,
          useFactory: HttpLoaderFactory,
          deps: [HttpClient]
        }
      })
    ),

    provideAnimations(),
    provideClientHydration(),
    provideHttpClient(withFetch(), withInterceptorsFromDi()),

    provideAppInitializer(() => {
      const initializerFn = initializeApp(inject(TranslationService), inject(AuthService));
      return initializerFn();
    }),

    provideAppInitializer(() => {
      inject(DeploymentVersionService).initialize();
    }),

    provideAppInitializer(() => {
      inject(MatomoPageViewTrackingService).initialize();
      inject(MicrosoftClarityTrackingService).initialize();
    }),

    {
      provide: HTTP_INTERCEPTORS,
      useClass: LanguageInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    },

    MessageService,

    providePrimeNG({
      theme: {
        preset: AmusementParkPreset,
        options: {
          darkModeSelector: '.dark-mode'
        }
      }
    })
  ]
};
