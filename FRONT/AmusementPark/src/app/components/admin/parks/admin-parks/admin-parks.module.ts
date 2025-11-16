import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AdminParksRoutingModule } from './admin-parks-routing.module';
import { AdminParksComponent } from './admin-parks.component';


@NgModule({
  declarations: [
    AdminParksComponent
  ],
  imports: [
    CommonModule,
    AdminParksRoutingModule
  ]
})
export class AdminParksModule { }
