import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

import { AdminUsersRoutingModule } from './admin-users-routing.module';
import { AdminUsersComponent } from './admin-users.component';
import { AdminUserManagementComponent } from '../admin-user-management/admin-user-management.component';

import { TableModule } from '@shared/primeless/table';
import { TranslateModule } from '@ngx-translate/core';
import { TagModule } from '@shared/primeless/tag';
import { CardModule } from '@shared/primeless/card';
import { ButtonModule } from '@shared/primeless/button';
import { TooltipModule } from '@shared/primeless/tooltip';
import { InputTextModule } from '@shared/primeless/inputtext';
import { SelectModule } from '@shared/primeless/select';

@NgModule({
    imports: [
    CommonModule,
    ReactiveFormsModule,
    AdminUsersRoutingModule,
    TableModule,
    TranslateModule,
    TagModule,
    CardModule,
    ButtonModule,
    TooltipModule,
    InputTextModule,
    SelectModule,
    AdminUsersComponent,
    AdminUserManagementComponent
]
})
export class AdminUsersModule {
}
