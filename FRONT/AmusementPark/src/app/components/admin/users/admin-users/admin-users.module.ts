import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AdminUsersRoutingModule } from './admin-users-routing.module';
import { AdminUsersComponent } from './admin-users.component';
import { ProfileModule } from '../../../login-register/profile/profile.module';
import { TableModule } from 'primeng/table';
import { TranslateModule } from '@ngx-translate/core';
import { TagModule } from 'primeng/tag';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';

@NgModule({
  declarations: [
    AdminUsersComponent
  ],
  imports: [
    CommonModule,
    AdminUsersRoutingModule,
    ProfileModule,
    TableModule,
    TranslateModule,
    TagModule,
    CardModule,
    ButtonModule,
    AvatarModule
  ]
})
export class AdminUsersModule {
}
