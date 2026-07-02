import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AdminParksRoutingModule } from './admin-parks-routing.module';
import { AdminParksComponent } from './admin-parks.component';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from '@shared/ui/primitives/button';
import { TableModule } from '@shared/ui/primitives/table';
import { CardModule } from '@shared/ui/primitives/card';
import { InputTextModule } from '@shared/ui/primitives/inputtext';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ToggleSwitchModule } from '@shared/ui/primitives/toggleswitch';
import { AdminParkEditComponent } from './admin-park-edit/admin-park-edit.component';
import { SelectModule } from '@shared/ui/primitives/select';
import { TagModule } from '@shared/ui/primitives/tag';

import { PaginatorModule } from '@shared/ui/primitives/paginator';
import { PanelModule } from '@shared/ui/primitives/panel';
import { DividerModule } from '@shared/ui/primitives/divider';
import { ProgressSpinnerModule } from '@shared/ui/primitives/progressspinner';
import { TabsModule } from '@shared/ui/primitives/tabs';
import {AdminFounderEditComponent} from "@features/admin/founders/pages/admin-founder-edit/admin-founder-edit.component";
import { AdminParkZonesComponent } from './admin-park-zones/admin-park-zones.component';
import { AdminParkZoneEditComponent } from './admin-park-zone-edit/admin-park-zone-edit.component';
import { AdminParkItemsComponent } from './admin-park-items/admin-park-items.component';
import { AdminParkItemEditComponent } from './admin-park-item-edit/admin-park-item-edit.component';
import { AdminParkItemEditFormComponent } from './admin-park-item-edit/admin-park-item-edit-form.component';
import { AdminParkItemGeneralTabComponent } from './admin-park-item-edit/tabs/admin-park-item-general-tab/admin-park-item-general-tab.component';
import { AdminParkItemDetailsTabComponent } from './admin-park-item-edit/tabs/admin-park-item-details-tab/admin-park-item-details-tab.component';
import { AdminParkItemAccessConditionsTabComponent } from './admin-park-item-edit/tabs/admin-park-item-access-conditions-tab/admin-park-item-access-conditions-tab.component';
import { AdminParkItemLocationsTabComponent } from './admin-park-item-edit/tabs/admin-park-item-locations-tab/admin-park-item-locations-tab.component';
import { AdminParkItemPhotosTabComponent } from './admin-park-item-edit/tabs/admin-park-item-photos-tab/admin-park-item-photos-tab.component';
import { AdminParkGeneralTabComponent } from './admin-park-edit/tabs/admin-park-general-tab/admin-park-general-tab.component';
import { AdminParkLocationTabComponent } from './admin-park-edit/tabs/admin-park-location-tab/admin-park-location-tab.component';
import { AdminParkDescriptionsTabComponent } from './admin-park-edit/tabs/admin-park-descriptions-tab/admin-park-descriptions-tab.component';
import { AdminParkLogosTabComponent } from './admin-park-edit/tabs/admin-park-logos-tab/admin-park-logos-tab.component';
import { AdminParkPhotosTabComponent } from './admin-park-edit/tabs/admin-park-photos-tab/admin-park-photos-tab.component';

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
    AdminParkItemEditFormComponent,
    AdminParkItemGeneralTabComponent,
    AdminParkItemDetailsTabComponent,
    AdminParkItemAccessConditionsTabComponent,
    AdminParkItemLocationsTabComponent,
    AdminParkItemPhotosTabComponent,
    AdminParkGeneralTabComponent,
    AdminParkLocationTabComponent,
    AdminParkDescriptionsTabComponent,
    AdminParkLogosTabComponent,
    AdminParkPhotosTabComponent
]
})
export class AdminParksModule { }
