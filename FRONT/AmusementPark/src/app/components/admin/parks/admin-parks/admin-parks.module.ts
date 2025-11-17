import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AdminParksRoutingModule } from './admin-parks-routing.module';
import { AdminParksComponent } from './admin-parks.component';
import {TranslateModule} from "@ngx-translate/core";
import {ButtonModule} from "primeng/button";
import {TableModule} from "primeng/table";
import {CardModule} from "primeng/card";
import {InputTextModule} from "primeng/inputtext";
import {FormsModule, ReactiveFormsModule} from "@angular/forms";
import {InputSwitchModule} from "primeng/inputswitch";
import { AdminParkEditComponent } from './admin-park-edit/admin-park-edit.component';
import {DropdownModule} from "primeng/dropdown";
import {TagModule} from "primeng/tag";


@NgModule({
  declarations: [
    AdminParksComponent,
    AdminParkEditComponent
  ],
  imports: [
    CommonModule,
    AdminParksRoutingModule,
    CardModule,
    TableModule,
    ButtonModule,
    TranslateModule,
    InputTextModule,
    FormsModule,
    InputSwitchModule,
    DropdownModule,
    TagModule,
    ReactiveFormsModule
  ]
})
export class AdminParksModule { }
