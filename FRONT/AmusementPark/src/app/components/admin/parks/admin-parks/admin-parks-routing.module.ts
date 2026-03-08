import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminParksComponent } from './admin-parks.component';
import { AdminParkEditComponent } from './admin-park-edit/admin-park-edit.component';
import {AdminFounderEditComponent} from "../../operators/admin-founder-edit/admin-founder-edit.component";

const routes: Routes = [
  { path: '', component: AdminParksComponent },
  { path: 'new', component: AdminParkEditComponent },
  { path: 'edit/:idPark', component: AdminParkEditComponent },
  { path: 'founders/new', component: AdminFounderEditComponent },
  { path: 'founders/edit/:id', component: AdminFounderEditComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminParksRoutingModule { }
