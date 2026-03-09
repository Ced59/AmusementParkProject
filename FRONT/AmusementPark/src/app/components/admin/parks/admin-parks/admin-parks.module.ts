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
import { TabViewModule } from 'primeng/tabview';
import {AdminFounderEditComponent} from "../../operators/admin-founder-edit/admin-founder-edit.component";
import { AdminParkZonesComponent } from './admin-park-zones/admin-park-zones.component';
import { AdminParkZoneEditComponent } from './admin-park-zone-edit/admin-park-zone-edit.component';
import { AdminParkItemsComponent } from './admin-park-items/admin-park-items.component';
import { AdminParkItemEditComponent } from './admin-park-item-edit/admin-park-item-edit.component';

@NgModule({
  declarations: [
    AdminParksComponent,
    AdminParkEditComponent,
    AdminFounderEditComponent,
    AdminParkZonesComponent,
    AdminParkZoneEditComponent,
    AdminParkItemsComponent,
    AdminParkItemEditComponent
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
    ProgressSpinnerModule,
    TabViewModule
  ]
})
export class AdminParksModule { }
