import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ParkContentSummaryViewModel } from '../models/park-content-summary.model';
import { UiButtonDirective, UiSectionHeaderComponent } from '@ui/primitives';
import { LocalizedPluralPipe } from '@shared/pipes';

@Component({
  selector: 'app-park-content-summary',
  templateUrl: './park-content-summary.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: ['./park-content-summary.component.scss'],
  imports: [NgFor, NgIf, RouterLink, TranslateModule, UiButtonDirective, UiSectionHeaderComponent, LocalizedPluralPipe]
})
export class ParkContentSummaryComponent {
  @Input() summary: ParkContentSummaryViewModel | null = null;
}
