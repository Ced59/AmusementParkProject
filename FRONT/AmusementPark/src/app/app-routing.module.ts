import { NgModule, inject } from '@angular/core';
import {RouterModule, Routes, CanActivateFn, CanMatchFn} from '@angular/router';
import { HomeComponent } from './components/home/home.component';
import { AboutComponent } from './components/about/about.component';
import { TranslationService } from './services/translation.service';
import { ActivatedRouteSnapshot, Route } from '@angular/router';

const languageGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const translationService = inject(TranslationService);
  const lang = route.paramMap.get('lang') || 'en';
  translationService.useLang(lang);
  return true;
};

const languageMatcher: CanMatchFn = (route: Route) => {
  const lang = route.path?.split('/')[0] || 'en';
  const translationService = inject(TranslationService);
  translationService.useLang(lang);
  return true;
};

const routes: Routes = [
  {
    path: '',
    redirectTo: 'en/home',
    pathMatch: 'full'
  },
  {
    path: ':lang',
    canActivate: [languageGuard],
    canMatch: [languageMatcher],
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', component: HomeComponent },
      { path: 'about', component: AboutComponent }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
  providers: [
    {
      provide: 'languageGuard',
      useFactory: () => languageGuard
    },
    {
      provide: 'languageMatcher',
      useFactory: () => languageMatcher
    }
  ]
})
export class AppRoutingModule { }
