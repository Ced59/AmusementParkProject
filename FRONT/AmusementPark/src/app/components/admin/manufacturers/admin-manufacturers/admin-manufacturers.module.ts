import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';


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
