import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AdminSiteRoutingModule } from './admin-site-routing.module';
import { AdminSiteComponent } from './admin-site.component';


@NgModule({
  declarations: [
    AdminSiteComponent
  ],
  imports: [
    CommonModule,
    AdminSiteRoutingModule
  ]
})
export class AdminSiteModule { }
