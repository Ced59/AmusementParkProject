import { initializeApp, HttpLoaderFactory } from './app/app.module';
import { provideHttpClient, withFetch, withInterceptorsFromDi, HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { provideAppInitializer, inject, importProvidersFrom } from '@angular/core';
import { TranslationService } from './app/services/translation.service';
import { LanguageInterceptor } from './app/interceptors/language.interceptor';
import { AuthInterceptor } from './app/interceptors/auth.interceptor';
import { MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';
import { BrowserModule, bootstrapApplication } from '@angular/platform-browser';
import { AppRoutingModule } from './app/app-routing.module';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
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
import { AppComponent } from './app/app.component';
import AmusementParkPreset from './app/config/primeng-preset';


bootstrapApplication(AppComponent, {
    providers: [
        importProvidersFrom(BrowserModule, AppRoutingModule, TranslateModule.forRoot({
            loader: {
                provide: TranslateLoader,
                useFactory: HttpLoaderFactory,
                deps: [HttpClient]
            }
        }), BrowserAnimationsModule, SelectModule, ToolbarModule, ButtonModule, FormsModule, DialogModule, InputTextModule, CardModule, TooltipModule, ToastModule, AvatarModule, PaginatorModule, MultiSelectModule),
        provideHttpClient(withFetch(), withInterceptorsFromDi()),
        provideAppInitializer(() => {
            const initializerFn = initializeApp(inject(TranslationService));
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
})
  .catch(err => console.error(err));
