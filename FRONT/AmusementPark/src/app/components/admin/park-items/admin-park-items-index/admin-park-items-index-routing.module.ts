import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AdminParkItemsIndexComponent } from './admin-park-items-index.component';

const routes: Routes = [
  { path: '', component: AdminParkItemsIndexComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminParkItemsIndexRoutingModule { }
