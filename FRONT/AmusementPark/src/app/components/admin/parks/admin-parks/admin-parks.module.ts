import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AdminParksRoutingModule } from './admin-parks-routing.module';
import { AdminParksComponent } from './admin-parks.component';
import {TranslateModule} from "@ngx-translate/core";
import {ButtonModule} from "primeng/button";
import {TableModule} from "primeng/table";
import {CardModule} from "primeng/card";


@NgModule({
  declarations: [
    AdminParksComponent
  ],
  imports: [
    CommonModule,
    AdminParksRoutingModule,
    CardModule,
    TableModule,
    ButtonModule,
    TranslateModule
  ]
})
export class AdminParksModule { }
