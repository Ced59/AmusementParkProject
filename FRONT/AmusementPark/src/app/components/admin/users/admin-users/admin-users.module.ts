import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

import { AdminUsersRoutingModule } from './admin-users-routing.module';
import { AdminUsersComponent } from './admin-users.component';
import { AdminUserManagementComponent } from '../admin-user-management/admin-user-management.component';
import { SharedModule } from '../../../shared/shared.module';
import { TableModule } from 'primeng/table';
import { TranslateModule } from '@ngx-translate/core';
import { TagModule } from 'primeng/tag';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { TooltipModule } from 'primeng/tooltip';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';

@NgModule({
  declarations: [
    AdminUsersComponent,
    AdminUserManagementComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    AdminUsersRoutingModule,
    SharedModule,
    TableModule,
    TranslateModule,
    TagModule,
    CardModule,
    ButtonModule,
    AvatarModule,
    TooltipModule,
    InputTextModule,
    SelectModule
  ]
})
export class AdminUsersModule {
}
