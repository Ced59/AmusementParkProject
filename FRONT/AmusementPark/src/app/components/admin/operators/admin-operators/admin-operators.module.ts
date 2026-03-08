import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';

import { SharedModule } from '../../../shared/shared.module';
import { AdminOperatorsRoutingModule } from './admin-operators-routing.module';
import { AdminOperatorsComponent } from './admin-operators.component';
import {AdminOperatorEditComponent} from "../admin-operator-edit/admin-operator-edit.component";
import {TranslateModule} from "@ngx-translate/core";


@NgModule({
  declarations: [
    AdminOperatorsComponent,
    AdminOperatorEditComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    TableModule,
    SharedModule,
    AdminOperatorsRoutingModule,
    TranslateModule
  ]
})
export class AdminOperatorsModule { }
