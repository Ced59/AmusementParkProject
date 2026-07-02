import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from '@shared/primeless/button';
import { CardModule } from '@shared/primeless/card';
import { InputTextModule } from '@shared/primeless/inputtext';
import { TableModule } from '@shared/primeless/table';


import { TranslateModule } from '@ngx-translate/core';
import { AdminManufacturersRoutingModule } from './admin-manufacturers-routing.module';
import { AdminManufacturersComponent } from './admin-manufacturers.component';
import { AdminManufacturerEditComponent } from '../admin-manufacturer-edit/admin-manufacturer-edit.component';

@NgModule({
    imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    TableModule,
    TranslateModule,
    AdminManufacturersRoutingModule,
    AdminManufacturersComponent,
    AdminManufacturerEditComponent
]
})
export class AdminManufacturersModule { }
