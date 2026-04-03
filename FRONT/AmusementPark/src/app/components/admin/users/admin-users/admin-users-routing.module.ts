import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AdminUsersComponent } from './admin-users.component';
import { ProfilePageComponent } from '../../../login-register/profile/profile-page/profile-page.component';

const routes: Routes = [
  { path: '', component: AdminUsersComponent },
  { path: ':id', component: ProfilePageComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminUsersRoutingModule {
}
