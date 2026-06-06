import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';



const routes: Routes = [
  { path: '', loadComponent: () => import('./admin-manufacturers.component').then(m => m.AdminManufacturersComponent) },
  { path: 'new', loadComponent: () => import('../admin-manufacturer-edit/admin-manufacturer-edit.component').then(m => m.AdminManufacturerEditComponent) },
  { path: 'edit/:id', loadComponent: () => import('../admin-manufacturer-edit/admin-manufacturer-edit.component').then(m => m.AdminManufacturerEditComponent) }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminManufacturersRoutingModule { }
