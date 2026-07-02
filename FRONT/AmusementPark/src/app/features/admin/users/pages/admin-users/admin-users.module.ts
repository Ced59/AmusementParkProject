import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

import { AdminUsersRoutingModule } from './admin-users-routing.module';
import { AdminUsersComponent } from './admin-users.component';
import { AdminUserManagementComponent } from '../admin-user-management/admin-user-management.component';

import { TableModule } from 'primeng/table';
import { TranslateModule } from '@ngx-translate/core';
import { TagModule } from 'primeng/tag';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';

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
