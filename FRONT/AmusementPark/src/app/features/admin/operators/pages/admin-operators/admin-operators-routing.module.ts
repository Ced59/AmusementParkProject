import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';




const routes: Routes = [
  { path: '', loadComponent: () => import('./admin-operators.component').then(m => m.AdminOperatorsComponent) },
  { path: 'new', loadComponent: () => import('../admin-operator-edit/admin-operator-edit.component').then(m => m.AdminOperatorEditComponent) },
  { path: 'edit/:id', loadComponent: () => import('../admin-operator-edit/admin-operator-edit.component').then(m => m.AdminOperatorEditComponent) }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminOperatorsRoutingModule { }
