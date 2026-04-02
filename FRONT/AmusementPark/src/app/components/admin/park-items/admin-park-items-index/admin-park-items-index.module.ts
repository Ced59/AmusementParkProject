import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { PaginatorModule } from 'primeng/paginator';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';

import { AdminParkItemsIndexComponent } from './admin-park-items-index.component';
import { AdminParkItemsIndexRoutingModule } from './admin-park-items-index-routing.module';

@NgModule({
  declarations: [AdminParkItemsIndexComponent],
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    ButtonModule,
    CardModule,
    SelectModule,
    InputTextModule,
    PaginatorModule,
    TableModule,
    TagModule,
    AdminParkItemsIndexRoutingModule
  ]
})
export class AdminParkItemsIndexModule { }
