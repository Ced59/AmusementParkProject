import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from '@shared/ui/primitives/button';
import { CardModule } from '@shared/ui/primitives/card';
import { SelectModule } from '@shared/ui/primitives/select';
import { InputTextModule } from '@shared/ui/primitives/inputtext';
import { PaginatorModule } from '@shared/ui/primitives/paginator';
import { TableModule } from '@shared/ui/primitives/table';
import { TagModule } from '@shared/ui/primitives/tag';

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
