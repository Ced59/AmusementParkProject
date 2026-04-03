import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { ProfileRoutingModule } from './profile-routing.module';
import { ProfilePageModule } from './profile-page.module';

@NgModule({
  imports: [
    CommonModule,
    ProfileRoutingModule,
    ProfilePageModule
  ]
})
export class ProfileModule {
}
