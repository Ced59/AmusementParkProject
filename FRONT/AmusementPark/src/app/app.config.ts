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
import { provideRouter } from '@angular/router';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { MatomoConsentRequirement, provideMatomo, withRouter } from 'ngx-matomo-client';
import { providePrimeNG } from 'primeng/config';

import { routes } from './app.routes';
import { initializeApp, HttpLoaderFactory } from './app.module';
import { TranslationService } from './services/translation.service';
import { AuthService } from './services/auth/auth.service';
import { LanguageInterceptor } from '@core/http/interceptors/language.interceptor';
import { AuthInterceptor } from '@core/http/interceptors/auth.interceptor';
import AmusementParkPreset from './config/primeng-preset';
import { environment } from '../environments/environment';

import { SelectModule } from 'primeng/select';
import { ToolbarModule } from 'primeng/toolbar';
import { ButtonModule } from 'primeng/button';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { AvatarModule } from 'primeng/avatar';
import { PaginatorModule } from 'primeng/paginator';
import { MultiSelectModule } from 'primeng/multiselect';


const matomoConsentRequirement: MatomoConsentRequirement = environment.analytics.matomoRequireConsent ? 'tracking' : 'none';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideMatomo(
      {
        siteId: environment.analytics.matomoSiteId,
        trackerUrl: environment.analytics.matomoTrackerUrl,
        disabled: !environment.analytics.matomoEnabled,
        requireConsent: matomoConsentRequirement,
        acceptDoNotTrack: true,
        enableLinkTracking: true,
        runOutsideAngularZone: true,
        disableCampaignParameters: true,
        excludedQueryParams: ['token', 'code', 'email', 'returnUrl', 'resetToken', 'confirmationToken']
      },
      withRouter({
        delay: 250,
        exclude: [/\/[a-z]{2}\/admin(?:\/|$)/i]
      })
    ),

    importProvidersFrom(
      TranslateModule.forRoot({
        loader: {
          provide: TranslateLoader,
          useFactory: HttpLoaderFactory,
          deps: [HttpClient]
        }
      }),
      SelectModule,
      ToolbarModule,
      ButtonModule,
      FormsModule,
      DialogModule,
      InputTextModule,
      CardModule,
      TooltipModule,
      ToastModule,
      AvatarModule,
      PaginatorModule,
      MultiSelectModule
    ),

    provideAnimations(),
    provideClientHydration(),
    provideHttpClient(withFetch(), withInterceptorsFromDi()),

    provideAppInitializer(() => {
      const initializerFn = initializeApp(inject(TranslationService), inject(AuthService));
      return initializerFn();
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
