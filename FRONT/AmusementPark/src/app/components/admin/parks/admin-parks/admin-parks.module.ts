import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AdminParksRoutingModule } from './admin-parks-routing.module';
import { AdminParksComponent } from './admin-parks.component';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { InputSwitchModule } from 'primeng/inputswitch';
import { AdminParkEditComponent } from './admin-park-edit/admin-park-edit.component';
import { DropdownModule } from 'primeng/dropdown';
import { TagModule } from 'primeng/tag';
import { SharedModule } from '../../../shared/shared.module';
import { PaginatorModule } from 'primeng/paginator';
import { PanelModule } from 'primeng/panel';
import { DividerModule } from 'primeng/divider';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import {AdminFounderEditComponent} from "../../operators/admin-founder-edit/admin-founder-edit.component";

@NgModule({
  declarations: [
    AdminParksComponent,
    AdminParkEditComponent,
    AdminFounderEditComponent
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
    ReactiveFormsModule,
    SharedModule,
    PaginatorModule,
    PanelModule,
    DividerModule,
    ProgressSpinnerModule
  ]
})
export class AdminParksModule { }
