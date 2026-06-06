import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./admin-founders.component').then(m => m.AdminFoundersComponent) },
  { path: 'new', loadComponent: () => import('@features/admin/founders/pages/admin-founder-edit/admin-founder-edit.component').then(m => m.AdminFounderEditComponent) },
  { path: 'edit/:id', loadComponent: () => import('@features/admin/founders/pages/admin-founder-edit/admin-founder-edit.component').then(m => m.AdminFounderEditComponent) }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminFoundersRoutingModule { }
