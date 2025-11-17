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
import {PaginatorModule} from "primeng/paginator";
import { ThemeSwitcherComponent } from './components/theme-switcher/theme-switcher.component';
import {ThemeService} from "./services/themes/themes.service";
import { ParkDetailComponent } from './components/park-detail/park-detail.component';
import { SidebarComponent } from './components/sidebar/sidebar.component';
import {SidebarModule} from "primeng/sidebar";
import { ParkListComponent } from './components/park-list/park-list.component';
import {MultiSelectModule} from "primeng/multiselect";
import { AdminDashboardComponent } from './components/admin/admin-dashboard/admin-dashboard.component';
import { LeafletMapComponent } from './components/shared/leaflet-map/leaflet-map.component';
import {SharedModule} from "./components/shared/shared.module";

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
      SigninGoogleComponent,
      ThemeSwitcherComponent,
      ParkDetailComponent,
      SidebarComponent,
      ParkListComponent,
      AdminDashboardComponent
    ],
    bootstrap: [AppComponent],
    imports: [
      BrowserModule,
      AppRoutingModule,
      SharedModule,
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
      AvatarModule, PaginatorModule, SidebarModule, MultiSelectModule],
    exports: [
      SidebarComponent
    ],
    providers: [
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
