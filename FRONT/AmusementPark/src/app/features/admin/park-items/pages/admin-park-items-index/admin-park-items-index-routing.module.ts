import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';



const routes: Routes = [
  { path: '', loadComponent: () => import('./admin-park-items-index.component').then(m => m.AdminParkItemsIndexComponent) }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminParkItemsIndexRoutingModule { }
