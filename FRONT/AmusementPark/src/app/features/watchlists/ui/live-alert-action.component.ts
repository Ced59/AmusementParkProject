import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { PublicLiveTarget } from '@app/models/live-data/public-live.models';
import { LiveAlertSubscription, LiveAlertType } from '@app/models/watchlists/live-alert.model';
import { UiButtonDirective, UiChipComponent, UiSurfaceDirective } from '@ui/primitives';
import { LiveAlertActionsFacade } from '../state/live-alert-actions.facade';

@Component({
  selector: 'app-live-alert-action',
  templateUrl: './live-alert-action.component.html',
  styleUrl: './live-alert-action.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [LiveAlertActionsFacade],
  imports: [DatePipe, TranslateModule, UiButtonDirective, UiChipComponent, UiSurfaceDirective]
})
export class LiveAlertActionComponent implements OnChanges {
  @Input({ required: true }) target!: PublicLiveTarget;

  protected readonly types: readonly LiveAlertType[] = ['Reopened', 'WaitBelow', 'WaitAbove', 'Degraded'];
  protected readonly durations: readonly number[] = [60, 180, 360, 720];

  constructor(protected readonly facade: LiveAlertActionsFacade) {
  }

  ngOnChanges(_changes: SimpleChanges): void {
    if (this.target) {
      this.facade.configure(this.target);
    }
  }

  protected canCreate(): boolean {
    return this.target.availability === 'Current' && this.target.freshness === 'Fresh';
  }

  protected typeLabelKey(type: LiveAlertType): string {
    return `liveAlerts.types.${type}.label`;
  }

  protected subscriptionLabelKey(subscription: LiveAlertSubscription): string {
    return `liveAlerts.types.${subscription.type}.active`;
  }

  protected onThresholdChanged(event: Event): void {
    this.facade.setThreshold(Number((event.target as HTMLInputElement).value));
  }
}
