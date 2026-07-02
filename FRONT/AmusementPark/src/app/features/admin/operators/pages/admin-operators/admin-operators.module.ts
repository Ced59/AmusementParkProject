import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from '@shared/primeless/button';
import { CardModule } from '@shared/primeless/card';
import { InputTextModule } from '@shared/primeless/inputtext';
import { TableModule } from '@shared/primeless/table';


import { AdminOperatorsRoutingModule } from './admin-operators-routing.module';
import { AdminOperatorsComponent } from './admin-operators.component';
import {AdminOperatorEditComponent} from "../admin-operator-edit/admin-operator-edit.component";
import {TranslateModule} from "@ngx-translate/core";


@NgModule({
    imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    TableModule,
    AdminOperatorsRoutingModule,
    TranslateModule,
    AdminOperatorsComponent,
    AdminOperatorEditComponent
]
})
export class AdminOperatorsModule { }
