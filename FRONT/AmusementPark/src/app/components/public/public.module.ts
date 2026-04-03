import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';

import { SharedModule } from '../shared/shared.module';
import { ParkCardComponent } from './park-card/park-card.component';
import { SearchResultCardComponent } from './search-result-card/search-result-card.component';
import { ParkHeroSectionComponent } from './park-hero-section/park-hero-section.component';
import { ParkPracticalInfoSectionComponent } from './park-practical-info-section/park-practical-info-section.component';
import { ParkLocationSectionComponent } from './park-location-section/park-location-section.component';
import { ParkNearbySectionComponent } from './park-nearby-section/park-nearby-section.component';

@NgModule({
    imports: [
        CommonModule,
        RouterModule,
        TranslateModule,
        ButtonModule,
        SharedModule,
        ParkCardComponent,
        SearchResultCardComponent,
        ParkHeroSectionComponent,
        ParkPracticalInfoSectionComponent,
        ParkLocationSectionComponent,
        ParkNearbySectionComponent
    ],
    exports: [
        ParkCardComponent,
        SearchResultCardComponent,
        ParkHeroSectionComponent,
        ParkPracticalInfoSectionComponent,
        ParkLocationSectionComponent,
        ParkNearbySectionComponent
    ]
})
export class PublicModule {
}
