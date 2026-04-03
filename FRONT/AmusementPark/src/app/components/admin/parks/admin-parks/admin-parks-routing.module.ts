import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminParksComponent } from './admin-parks.component';
import { AdminParkEditComponent } from './admin-park-edit/admin-park-edit.component';
import {AdminFounderEditComponent} from "../../operators/admin-founder-edit/admin-founder-edit.component";
import { AdminParkZonesComponent } from './admin-park-zones/admin-park-zones.component';
import { AdminParkZoneEditComponent } from './admin-park-zone-edit/admin-park-zone-edit.component';
import { AdminParkItemsComponent } from './admin-park-items/admin-park-items.component';
import { AdminParkItemEditComponent } from './admin-park-item-edit/admin-park-item-edit.component';

const routes: Routes = [
  { path: '', component: AdminParksComponent },
  { path: 'new', component: AdminParkEditComponent },
  { path: 'edit/:idPark', component: AdminParkEditComponent },
  { path: 'founders/new', component: AdminFounderEditComponent },
  { path: 'founders/edit/:id', component: AdminFounderEditComponent },
  { path: 'edit/:idPark/zones', component: AdminParkZonesComponent },
  { path: 'edit/:idPark/zones/new', component: AdminParkZoneEditComponent },
  { path: 'edit/:idPark/zones/:idZone', component: AdminParkZoneEditComponent },
  { path: 'edit/:idPark/items', component: AdminParkItemsComponent },
  { path: 'edit/:idPark/items/new', component: AdminParkItemEditComponent },
  { path: 'edit/:idPark/items/:idItem', component: AdminParkItemEditComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminParksRoutingModule { }
