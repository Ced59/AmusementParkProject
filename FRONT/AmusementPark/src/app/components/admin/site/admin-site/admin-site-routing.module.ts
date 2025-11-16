import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import {AdminSiteComponent} from "./admin-site.component";

const routes: Routes = [
  { path: '', component: AdminSiteComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminSiteRoutingModule { }
