import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminManufacturersComponent } from './admin-manufacturers.component';
import { AdminManufacturerEditComponent } from '../admin-manufacturer-edit/admin-manufacturer-edit.component';

const routes: Routes = [
  { path: '', component: AdminManufacturersComponent },
  { path: 'new', component: AdminManufacturerEditComponent },
  { path: 'edit/:id', component: AdminManufacturerEditComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminManufacturersRoutingModule { }
