import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { SharedModule } from '../../../shared/shared.module';
import { AdminUsersRoutingModule } from './admin-users-routing.module';
import { AdminUsersComponent } from './admin-users.component';

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
    SharedModule
  ]
})
export class AdminUsersModule {
}
