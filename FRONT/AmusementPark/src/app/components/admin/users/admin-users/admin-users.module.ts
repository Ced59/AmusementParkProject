import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AdminUsersRoutingModule } from './admin-users-routing.module';
import { AdminUsersComponent } from './admin-users.component';
import {TableModule} from "primeng/table";
import {TranslateModule} from "@ngx-translate/core";
import {TagModule} from "primeng/tag";
import {CardModule} from "primeng/card";


@NgModule({
  declarations: [
    AdminUsersComponent
  ],
  imports: [
    CommonModule,
    AdminUsersRoutingModule,
    TableModule,
    TranslateModule,
    TagModule,
    CardModule
  ]
})
export class AdminUsersModule { }
