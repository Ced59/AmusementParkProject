import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from '@shared/primeless/button';
import { CardModule } from '@shared/primeless/card';
import { SelectModule } from '@shared/primeless/select';
import { InputTextModule } from '@shared/primeless/inputtext';
import { PaginatorModule } from '@shared/primeless/paginator';
import { TableModule } from '@shared/primeless/table';
import { TagModule } from '@shared/primeless/tag';

import { AdminParkItemsIndexComponent } from './admin-park-items-index.component';
import { AdminParkItemsIndexRoutingModule } from './admin-park-items-index-routing.module';

@NgModule({
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
        AdminParkItemsIndexRoutingModule,
        AdminParkItemsIndexComponent
    ]
})
export class AdminParkItemsIndexModule { }
