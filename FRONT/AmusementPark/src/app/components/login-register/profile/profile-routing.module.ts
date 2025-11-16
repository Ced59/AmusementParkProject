import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import {ProfilePageComponent} from "./profile-page/profile-page.component";
import {authGuard} from "../../../guards/auth.guard";

const routes: Routes = [
  {
    path: '',
    component: ProfilePageComponent,
    canActivate: [authGuard]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ProfileRoutingModule { }
