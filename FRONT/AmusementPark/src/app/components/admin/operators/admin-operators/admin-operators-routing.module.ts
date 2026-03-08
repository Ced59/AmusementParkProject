import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminOperatorsComponent } from './admin-operators.component';
import {AdminOperatorEditComponent} from "../admin-operator-edit/admin-operator-edit.component";


const routes: Routes = [
  { path: '', component: AdminOperatorsComponent },
  { path: 'new', component: AdminOperatorEditComponent },
  { path: 'edit/:id', component: AdminOperatorEditComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminOperatorsRoutingModule { }
