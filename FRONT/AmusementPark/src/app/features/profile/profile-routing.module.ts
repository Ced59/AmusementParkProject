import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import {authGuard} from "@core/guards/auth.guard";

export const PROFILE_ROUTES: Routes = [
  {
    path: 'passport/items/:parkItemId',
    loadComponent: () => import('./passport/pages/passport-statistics-page/passport-statistics-page.component')
      .then((module) => module.PassportStatisticsPageComponent),
    canActivate: [authGuard],
    data: { passportStatisticsScope: 'item' }
  },
  {
    path: 'passport/parks/:parkId',
    loadComponent: () => import('./passport/pages/passport-statistics-page/passport-statistics-page.component')
      .then((module) => module.PassportStatisticsPageComponent),
    canActivate: [authGuard],
    data: { passportStatisticsScope: 'park' }
  },
  {
    path: 'passport/years/:year',
    loadComponent: () => import('./passport/pages/passport-statistics-page/passport-statistics-page.component')
      .then((module) => module.PassportStatisticsPageComponent),
    canActivate: [authGuard],
    data: { passportStatisticsScope: 'year' }
  },
  {
    path: 'visits/:visitId',
    loadComponent: () => import('./passport/pages/passport-visit-editor-page/passport-visit-editor-page.component')
      .then((module) => module.PassportVisitEditorPageComponent),
    canActivate: [authGuard]
  },
  {
    path: '',
    loadComponent: () => import('./pages/profile-page/profile-page.component').then(m => m.ProfilePageComponent),
    canActivate: [authGuard]
  }
];

@NgModule({
  imports: [RouterModule.forChild(PROFILE_ROUTES)],
  exports: [RouterModule]
})
export class ProfileRoutingModule { }
