import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UiButtonDirective } from '@ui/primitives';
import { PassportStatisticsTimelinePointViewModel } from '../../models/passport-statistics-view.models';

@Component({
  selector: 'app-passport-rating-timeline',
  templateUrl: './passport-rating-timeline.component.html',
  styleUrl: './passport-rating-timeline.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule, UiButtonDirective]
})
export class PassportRatingTimelineComponent {
  @Input({ required: true }) titleKey!: string;
  @Input({ required: true }) descriptionKey!: string;
  @Input({ required: true }) points: PassportStatisticsTimelinePointViewModel[] = [];
  @Output() visitSelected: EventEmitter<string> = new EventEmitter<string>();
}
