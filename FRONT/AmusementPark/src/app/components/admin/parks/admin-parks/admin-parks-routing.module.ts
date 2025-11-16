import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import {AdminParksComponent} from "./admin-parks.component";

const routes: Routes = [
  { path: '', component: AdminParksComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminParksRoutingModule { }
