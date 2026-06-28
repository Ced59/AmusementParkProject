import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';


const routes: Routes = [
  { path: 'batch', loadComponent: () => import('./admin-photo-batch/admin-photo-batch.component').then(m => m.AdminPhotoBatchComponent) },
  { path: '', loadComponent: () => import('./admin-site.component').then(m => m.AdminSiteComponent) }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminSiteRoutingModule { }
