import { ChangeDetectionStrategy, Component, effect, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { TripNotificationFacade } from '../../state/trip-notification.facade';

@Component({
  selector: 'app-trip-notification-panel',
  templateUrl: './trip-notification-panel.component.html',
  styleUrl: './trip-notification-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TripNotificationFacade],
  imports: [RouterLink, TranslateModule, UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective]
})
export class TripNotificationPanelComponent {
  readonly tripPlanId = input.required<string>();
  readonly currentLanguage = input.required<string>();

  constructor(protected readonly facade: TripNotificationFacade) {
    effect((): void => this.facade.load(this.tripPlanId()));
  }
}
