import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./admin-technical-pages.component').then((m) => m.AdminTechnicalPagesComponent) },
  { path: 'new', loadComponent: () => import('../admin-technical-page-edit/admin-technical-page-edit.component').then((m) => m.AdminTechnicalPageEditComponent) },
  { path: 'edit/:id', loadComponent: () => import('../admin-technical-page-edit/admin-technical-page-edit.component').then((m) => m.AdminTechnicalPageEditComponent) }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminTechnicalPagesRoutingModule { }
