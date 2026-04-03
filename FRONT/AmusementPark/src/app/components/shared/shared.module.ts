import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { LeafletMapComponent } from './leaflet-map/leaflet-map.component';
import { PageStateComponent } from './page-state/page-state.component';

@NgModule({
  declarations: [
    LeafletMapComponent,
    PageStateComponent
  ],
  imports: [
    CommonModule,
    TranslateModule
  ],
  exports: [
    LeafletMapComponent,
    PageStateComponent
  ]
})
export class SharedModule {
}
