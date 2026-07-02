import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

import { AdminUsersRoutingModule } from './admin-users-routing.module';
import { AdminUsersComponent } from './admin-users.component';
import { AdminUserManagementComponent } from '../admin-user-management/admin-user-management.component';

import { TableModule } from '@shared/ui/primitives/table';
import { TranslateModule } from '@ngx-translate/core';
import { TagModule } from '@shared/ui/primitives/tag';
import { CardModule } from '@shared/ui/primitives/card';
import { ButtonModule } from '@shared/ui/primitives/button';
import { TooltipModule } from '@shared/ui/primitives/tooltip';
import { InputTextModule } from '@shared/ui/primitives/inputtext';
import { SelectModule } from '@shared/ui/primitives/select';

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
