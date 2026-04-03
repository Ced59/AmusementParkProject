import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TranslateModule } from '@ngx-translate/core';

import { AdminUsersRoutingModule } from './admin-users-routing.module';
import { AdminUsersComponent } from './admin-users.component';
import { ProfilePageModule } from '../../../login-register/profile/profile-page.module';

@NgModule({
  declarations: [
    AdminUsersComponent
  ],
  imports: [
    CommonModule,
    AdminUsersRoutingModule,
    TableModule,
    TranslateModule,
    TagModule,
    CardModule,
    ButtonModule,
    ProfilePageModule
  ]
})
export class AdminUsersModule {
}
