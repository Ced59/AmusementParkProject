import { Component, Input } from '@angular/core';
import { Park } from '../../../models/parks/park';
import { ViewState } from '../../../models/shared/view-state';
import { PageStateComponent } from '../../shared/page-state/page-state.component';
import { NgFor } from '@angular/common';
import { ParkCardComponent } from '../park-card/park-card.component';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-park-nearby-section',
    templateUrl: './park-nearby-section.component.html',
    styleUrls: ['./park-nearby-section.component.scss'],
    imports: [PageStateComponent, NgFor, ParkCardComponent, TranslateModule]
})
export class ParkNearbySectionComponent {
  @Input() parks: Park[] = [];
  @Input() currentLang = 'en';
  @Input() state: ViewState = ViewState.Empty;
}
