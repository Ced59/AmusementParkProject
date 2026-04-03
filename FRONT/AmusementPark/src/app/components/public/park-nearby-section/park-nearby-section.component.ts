import { Component, Input } from '@angular/core';
import { Park } from '../../../models/parks/park';
import { ViewState } from '../../../models/shared/view-state';

@Component({
  selector: 'app-park-nearby-section',
  templateUrl: './park-nearby-section.component.html',
  styleUrls: ['./park-nearby-section.component.scss']
})
export class ParkNearbySectionComponent {
  @Input() parks: Park[] = [];
  @Input() currentLang = 'en';
  @Input() state: ViewState = ViewState.Empty;
}
