import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AdminSiteRoutingModule } from './admin-site-routing.module';
import { AdminSiteComponent } from './admin-site.component';


@NgModule({
    imports: [
        CommonModule,
        AdminSiteRoutingModule,
        AdminSiteComponent
    ]
})
export class AdminSiteModule { }
