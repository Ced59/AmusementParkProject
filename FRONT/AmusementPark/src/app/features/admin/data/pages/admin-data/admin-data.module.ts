import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AdminDataRoutingModule } from './admin-data-routing.module';
import { AdminDataComponent } from './admin-data.component';

@NgModule({
  imports: [
    CommonModule,
    AdminDataRoutingModule,
    AdminDataComponent
  ]
})
export class AdminDataModule {}
