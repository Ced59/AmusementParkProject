import { NgModule, APP_INITIALIZER } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule, HttpClient } from '@angular/common/http';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { HomeComponent } from './components/home/home.component';
import { AboutComponent } from './components/about/about.component';
import { TranslationService } from './services/translation.service';
import { TopbarComponent } from './components/topbar/topbar.component';
import {DropdownModule} from "primeng/dropdown";
import {ToolbarModule} from "primeng/toolbar";
import {ButtonModule} from "primeng/button";
import {FormsModule} from "@angular/forms";
import {BrowserAnimationsModule} from "@angular/platform-browser/animations";
import { LoginFormComponent } from './components/login-form/login-form.component';
import {DialogModule} from "primeng/dialog";

// Fonction pour créer un nouveau TranslateHttpLoader
export function HttpLoaderFactory(http: HttpClient) {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

// Initializer function
export function initializeApp(translationService: TranslationService): () => Promise<any> {
  return () => translationService.initializeLanguage();
}

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    AboutComponent,
    TopbarComponent,
    LoginFormComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
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
    DialogModule
  ],
  providers: [
    {
      provide: APP_INITIALIZER,
      useFactory: initializeApp,
      deps: [TranslationService],
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
