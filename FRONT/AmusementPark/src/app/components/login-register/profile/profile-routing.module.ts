import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import {authGuard} from "../../../guards/auth.guard";

const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./profile-page/profile-page.component').then(m => m.ProfilePageComponent),
    canActivate: [authGuard]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ProfileRoutingModule { }
