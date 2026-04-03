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
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { AdminParkEditComponent } from './admin-park-edit/admin-park-edit.component';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';

import { PaginatorModule } from 'primeng/paginator';
import { PanelModule } from 'primeng/panel';
import { DividerModule } from 'primeng/divider';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TabsModule } from 'primeng/tabs';
import {AdminFounderEditComponent} from "../../operators/admin-founder-edit/admin-founder-edit.component";
import { AdminParkZonesComponent } from './admin-park-zones/admin-park-zones.component';
import { AdminParkZoneEditComponent } from './admin-park-zone-edit/admin-park-zone-edit.component';
import { AdminParkItemsComponent } from './admin-park-items/admin-park-items.component';
import { AdminParkItemEditComponent } from './admin-park-item-edit/admin-park-item-edit.component';
import { AdminParkItemGeneralTabComponent } from './admin-park-item-edit/tabs/admin-park-item-general-tab/admin-park-item-general-tab.component';
import { AdminParkItemDetailsTabComponent } from './admin-park-item-edit/tabs/admin-park-item-details-tab/admin-park-item-details-tab.component';
import { AdminParkItemAccessConditionsTabComponent } from './admin-park-item-edit/tabs/admin-park-item-access-conditions-tab/admin-park-item-access-conditions-tab.component';
import { AdminParkItemLocationsTabComponent } from './admin-park-item-edit/tabs/admin-park-item-locations-tab/admin-park-item-locations-tab.component';
import { AdminParkItemPhotosTabComponent } from './admin-park-item-edit/tabs/admin-park-item-photos-tab/admin-park-item-photos-tab.component';
import { AdminParkGeneralTabComponent } from './admin-park-edit/tabs/admin-park-general-tab/admin-park-general-tab.component';
import { AdminParkLocationTabComponent } from './admin-park-edit/tabs/admin-park-location-tab/admin-park-location-tab.component';
import { AdminParkDescriptionsTabComponent } from './admin-park-edit/tabs/admin-park-descriptions-tab/admin-park-descriptions-tab.component';
import { AdminParkLogosTabComponent } from './admin-park-edit/tabs/admin-park-logos-tab/admin-park-logos-tab.component';

@NgModule({
    imports: [
    CommonModule,
    AdminParksRoutingModule,
    CardModule,
    TableModule,
    ButtonModule,
    TranslateModule,
    InputTextModule,
    FormsModule,
    ToggleSwitchModule,
    SelectModule,
    TagModule,
    ReactiveFormsModule,
    PaginatorModule,
    PanelModule,
    DividerModule,
    ProgressSpinnerModule,
    TabsModule,
    AdminParksComponent,
    AdminParkEditComponent,
    AdminFounderEditComponent,
    AdminParkZonesComponent,
    AdminParkZoneEditComponent,
    AdminParkItemsComponent,
    AdminParkItemEditComponent,
    AdminParkItemGeneralTabComponent,
    AdminParkItemDetailsTabComponent,
    AdminParkItemAccessConditionsTabComponent,
    AdminParkItemLocationsTabComponent,
    AdminParkItemPhotosTabComponent,
    AdminParkGeneralTabComponent,
    AdminParkLocationTabComponent,
    AdminParkDescriptionsTabComponent,
    AdminParkLogosTabComponent
]
})
export class AdminParksModule { }
