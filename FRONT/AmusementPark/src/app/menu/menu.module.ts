import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatSelectModule } from '@angular/material/select';
import { MenuBarComponent } from './menu-bar/menu-bar.component';
import { CountriesFlagsComponent } from '../common/countries-flags/countries-flags.component';

@NgModule({
  declarations: [MenuBarComponent],
  imports: [
    CommonModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatSelectModule,
    CountriesFlagsComponent
  ],
  exports: [MenuBarComponent]
})
export class MenuModule { }

