import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { ProfileRoutingModule } from './profile-routing.module';
import { ProfilePageComponent } from './profile-page/profile-page.component';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TranslateModule } from '@ngx-translate/core';


@NgModule({
    imports: [
    CommonModule,
    FormsModule,
    ProfileRoutingModule,
    CardModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    TranslateModule,
    ProfilePageComponent
]
})
export class ProfileModule { }
