import {NgModule} from '@angular/core';
import {RouterModule, Routes} from '@angular/router';
import {HomeComponent} from './components/home/home.component';
import {AboutComponent} from './components/about/about.component';
import {languageGuard} from './guards/language.guard';
import {SigninGoogleComponent} from "./components/login-register/signin-google/signin-google.component";
import {ParkDetailComponent} from "./components/park-detail/park-detail.component";

const routes: Routes = [
  {
    path: ':lang',
    canActivate: [languageGuard],
    children: [
      {path: 'home', component: HomeComponent},
      {path: 'about', component: AboutComponent},
      { path: 'signin-google', component: SigninGoogleComponent },
      { path: 'profile', loadChildren: () => import('./components/login-register/profile/profile.module').then(m => m.ProfileModule) },
      { path: 'park/:id/:slug', component: ParkDetailComponent },
      {path: '', redirectTo: 'home', pathMatch: 'full'}
    ]
  },
  {path: '**', redirectTo: 'en/home', pathMatch: 'full'} // Fallback route
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {
}
