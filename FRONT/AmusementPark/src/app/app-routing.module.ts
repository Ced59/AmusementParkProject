// app-routing.module.ts
import { NgModule } from '@angular/core';
import { CanActivateFn, RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './components/home/home.component';
import { AboutComponent } from './components/about/about.component';
import { TranslationService } from './services/translation.service';

export function languageGuardFactory(translationService: TranslationService): CanActivateFn {
  return (route) => {
    const lang = route.params['lang'] || 'en';
    translationService.useLang(lang);
    return true;
  };
}

const routes: Routes = [
  {
    path: '',
    redirectTo: 'en/home',  // Redirection par défaut vers anglais
    pathMatch: 'full'
  },
  {
    path: ':lang',
    canActivate: [languageGuardFactory],
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },  // Redirection vers home lorsque seulement la langue est spécifiée
      { path: 'home', component: HomeComponent },
      { path: 'about', component: AboutComponent },
      // autres sous-routes
    ]
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  providers: [
    {
      provide: 'languageGuard',
      useFactory: languageGuardFactory,
      deps: [TranslationService]
    }
  ],
  exports: [RouterModule]
})
export class AppRoutingModule { }
