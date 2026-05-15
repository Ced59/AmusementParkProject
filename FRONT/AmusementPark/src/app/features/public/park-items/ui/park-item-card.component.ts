import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

import { UiItemCardComponent } from '@ui/cards';
import { ParkItemCardViewModel } from '../models/park-item-card.model';

@Component({
  selector: 'app-park-item-card-view',
  templateUrl: './park-item-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [UiItemCardComponent]
})
export class ParkItemCardComponent {
  @Input({ required: true }) card!: ParkItemCardViewModel;
}
