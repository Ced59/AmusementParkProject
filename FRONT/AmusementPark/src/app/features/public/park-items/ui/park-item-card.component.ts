import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ParkItemCardViewModel } from '../models/park-item-card.model';
import { UiButtonDirective, UiChipComponent } from '@ui/primitives';

@Component({
  selector: 'app-park-item-card-view',
  templateUrl: './park-item-card.component.html',
  styleUrls: ['./park-item-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgFor, NgIf, RouterLink, TranslateModule, UiButtonDirective, UiChipComponent]
})
export class ParkItemCardComponent {
  @Input({ required: true }) card!: ParkItemCardViewModel;
}
