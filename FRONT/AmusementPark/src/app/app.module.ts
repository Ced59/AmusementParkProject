import { NgModule, inject, provideAppInitializer } from '@angular/core';
import { providePrimeNG } from 'primeng/config';
import AmusementParkPreset from './config/primeng-preset';
import { BrowserModule } from '@angular/platform-browser';
import {
  HttpClient,
  HTTP_INTERCEPTORS,
  provideHttpClient,
  withFetch,
  withInterceptorsFromDi
} from '@angular/common/http';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { HomeComponent } from './components/home/home.component';
import { AboutComponent } from './components/about/about.component';
import { TranslationService } from './services/translation.service';
import { TopbarComponent } from './components/topbar/topbar.component';
import { SelectModule } from 'primeng/select';
import { ToolbarModule } from 'primeng/toolbar';
import { ButtonModule } from 'primeng/button';
import { FormsModule } from '@angular/forms';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { LoginFormComponent } from './components/login-register/login-form/login-form.component';
import { DialogModule } from 'primeng/dialog';
import { AuthModalComponent } from './components/login-register/auth-modal/auth-modal.component';
import { RegisterFormComponent } from './components/login-register/register-form/register-form.component';
import { InputTextModule } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';
import { LanguageInterceptor } from './interceptors/language.interceptor';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { AvatarModule } from 'primeng/avatar';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { PaginatorModule } from 'primeng/paginator';
import { ThemeSwitcherComponent } from './components/theme-switcher/theme-switcher.component';
import { ParkDetailComponent } from './components/park-detail/park-detail.component';
import { SidebarComponent } from './components/sidebar/sidebar.component';
import { ParkListComponent } from './components/park-list/park-list.component';
import { ParkExplorerComponent } from './components/park-explorer/park-explorer.component';
import { MultiSelectModule } from 'primeng/multiselect';
import { AdminDashboardComponent } from './components/admin/admin-dashboard/admin-dashboard.component';
import { ConfirmAccountPageComponent } from './components/login-register/confirm-account-page/confirm-account-page.component';
import { ForgotPasswordPageComponent } from './components/login-register/forgot-password-page/forgot-password-page.component';
import { ResetPasswordPageComponent } from './components/login-register/reset-password-page/reset-password-page.component';
import { SharedModule } from './components/shared/shared.module';
import { PublicModule } from './components/public/public.module';

export function HttpLoaderFactory(http: HttpClient) {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

export function initializeApp(translationService: TranslationService): () => Promise<any> {
  return () => translationService.initializeLanguage();
}

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    AboutComponent,
    TopbarComponent,
    LoginFormComponent,
    AuthModalComponent,
    RegisterFormComponent,
    ConfirmAccountPageComponent,
    ForgotPasswordPageComponent,
    ResetPasswordPageComponent,
    ThemeSwitcherComponent,
    ParkDetailComponent,
    SidebarComponent,
    ParkListComponent,
    ParkExplorerComponent,
    AdminDashboardComponent
  ],
  bootstrap: [AppComponent],
  imports: [
    BrowserModule,
    AppRoutingModule,
    SharedModule,
    PublicModule,
    TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: HttpLoaderFactory,
        deps: [HttpClient]
      }
    }),
    BrowserAnimationsModule,
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
  ],
  exports: [
    SidebarComponent
  ],
  providers: [
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
export class AppModule {
}
