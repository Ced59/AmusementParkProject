import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { PassportStatisticCardViewModel } from '../../models/passport-statistics-view.models';

@Component({
  selector: 'app-passport-stat-card',
  templateUrl: './passport-stat-card.component.html',
  styleUrl: './passport-stat-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule]
})
export class PassportStatCardComponent {
  @Input({ required: true }) card!: PassportStatisticCardViewModel;
}
