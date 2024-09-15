import {NgModule, APP_INITIALIZER} from '@angular/core';
import {BrowserModule} from '@angular/platform-browser';
import {
  HttpClient,
  HTTP_INTERCEPTORS,
  provideHttpClient,
  withFetch,
  withInterceptorsFromDi
} from '@angular/common/http';
import {TranslateModule, TranslateLoader} from '@ngx-translate/core';
import {TranslateHttpLoader} from '@ngx-translate/http-loader';
import {AppComponent} from './app.component';
import {AppRoutingModule} from './app-routing.module';
import {HomeComponent} from './components/home/home.component';
import {AboutComponent} from './components/about/about.component';
import {TranslationService} from './services/translation.service';
import {TopbarComponent} from './components/topbar/topbar.component';
import {DropdownModule} from "primeng/dropdown";
import {ToolbarModule} from "primeng/toolbar";
import {ButtonModule} from "primeng/button";
import {FormsModule} from "@angular/forms";
import {BrowserAnimationsModule} from "@angular/platform-browser/animations";
import {LoginFormComponent} from './components/login-register/login-form/login-form.component';
import {DialogModule} from "primeng/dialog";
import {AuthModalComponent} from './components/login-register/auth-modal/auth-modal.component';
import {RegisterFormComponent} from './components/login-register/register-form/register-form.component';
import {InputTextModule} from "primeng/inputtext";
import {CardModule} from "primeng/card";
import {LanguageInterceptor} from "./interceptors/language.interceptor";
import {TooltipModule} from "primeng/tooltip";
import {ToastModule} from "primeng/toast";
import {MessageService} from "primeng/api";
import {MessagesModule} from "primeng/messages";
import {MessageModule} from "primeng/message";
import {AvatarModule} from "primeng/avatar";
import {AuthInterceptor} from "./interceptors/auth.interceptor";
import { SigninGoogleComponent } from './components/login-register/signin-google/signin-google.component';

export function HttpLoaderFactory(http: HttpClient) {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

export function initializeApp(translationService: TranslationService): () => Promise<any> {
  return () => translationService.initializeLanguage();
}

@NgModule(
  {
    declarations: [
      AppComponent,
      HomeComponent,
      AboutComponent,
      TopbarComponent,
      LoginFormComponent,
      AuthModalComponent,
      RegisterFormComponent,
      SigninGoogleComponent
    ],
    bootstrap: [AppComponent], imports: [BrowserModule,
      AppRoutingModule,
      TranslateModule.forRoot({
        loader: {
          provide: TranslateLoader,
          useFactory: HttpLoaderFactory,
          deps: [HttpClient]
        }
      }),
      BrowserAnimationsModule,
      DropdownModule,
      ToolbarModule,
      ButtonModule,
      FormsModule,
      DialogModule,
      InputTextModule,
      CardModule,
      TooltipModule,
      ToastModule,
      MessagesModule,
      MessageModule,
      ToastModule,
      AvatarModule], providers: [
      provideHttpClient(withFetch()),
      {
        provide: APP_INITIALIZER,
        useFactory: initializeApp,
        deps: [TranslationService],
        multi: true
      },
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
      provideHttpClient(withInterceptorsFromDi())
    ]
  })
export class AppModule {
}
