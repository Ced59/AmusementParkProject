import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import { ParkItemCardViewModel } from '../models/park-item-card.model';

@Component({
  selector: 'app-park-item-card-view',
  templateUrl: './park-item-card.component.html',
  styleUrls: ['./park-item-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgFor, NgIf, RouterLink, TranslateModule, ButtonDirective]
})
export class ParkItemCardComponent {
  @Input({ required: true }) card!: ParkItemCardViewModel;
}
