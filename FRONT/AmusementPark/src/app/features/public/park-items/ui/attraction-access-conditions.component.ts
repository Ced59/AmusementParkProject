import { NgFor, NgIf } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';
import { ParkItemAccessConditionViewModel } from '../models/park-item-detail-view.model';

@Component({
  selector: 'app-attraction-access-conditions',
  imports: [NgFor, NgIf, TranslateModule, UiSectionHeaderComponent, UiSurfaceDirective],
  templateUrl: './attraction-access-conditions.component.html',
  styleUrl: './attraction-access-conditions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AttractionAccessConditionsComponent {
  @Input({ required: true }) conditions: ParkItemAccessConditionViewModel[] = [];
}
