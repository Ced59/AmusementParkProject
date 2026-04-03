import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';




const routes: Routes = [
  { path: '', loadComponent: () => import('./admin-users.component').then(m => m.AdminUsersComponent) },
  { path: ':id', loadComponent: () => import('../admin-user-management/admin-user-management.component').then(m => m.AdminUserManagementComponent) }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminUsersRoutingModule {
}
