import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';








const routes: Routes = [
  { path: '', loadComponent: () => import('./admin-parks.component').then(m => m.AdminParksComponent) },
  { path: 'new', loadComponent: () => import('./admin-park-edit/admin-park-edit.component').then(m => m.AdminParkEditComponent) },
  { path: 'edit/:idPark', loadComponent: () => import('./admin-park-edit/admin-park-edit.component').then(m => m.AdminParkEditComponent) },
  { path: 'founders/new', loadComponent: () => import('@features/admin/founders/pages/admin-founder-edit/admin-founder-edit.component').then(m => m.AdminFounderEditComponent) },
  { path: 'founders/edit/:id', loadComponent: () => import('@features/admin/founders/pages/admin-founder-edit/admin-founder-edit.component').then(m => m.AdminFounderEditComponent) },
  { path: 'edit/:idPark/zones', loadComponent: () => import('./admin-park-zones/admin-park-zones.component').then(m => m.AdminParkZonesComponent) },
  { path: 'edit/:idPark/zones/new', loadComponent: () => import('./admin-park-zone-edit/admin-park-zone-edit.component').then(m => m.AdminParkZoneEditComponent) },
  { path: 'edit/:idPark/zones/:idZone', loadComponent: () => import('./admin-park-zone-edit/admin-park-zone-edit.component').then(m => m.AdminParkZoneEditComponent) },
  { path: 'edit/:idPark/items', loadComponent: () => import('./admin-park-items/admin-park-items.component').then(m => m.AdminParkItemsComponent) },
  { path: 'edit/:idPark/items/new', loadComponent: () => import('./admin-park-item-edit/admin-park-item-edit.component').then(m => m.AdminParkItemEditComponent) },
  { path: 'edit/:idPark/items/:idItem', loadComponent: () => import('./admin-park-item-edit/admin-park-item-edit.component').then(m => m.AdminParkItemEditComponent) }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminParksRoutingModule { }
