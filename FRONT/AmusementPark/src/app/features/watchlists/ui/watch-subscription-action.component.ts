import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UserCollectionTargetType } from '@app/models/watchlists/user-collection-entry.model';
import {
  FactualEventType,
  WatchEventGroup
} from '@app/models/watchlists/watch-subscription.model';
import { UiButtonDirective, UiSurfaceDirective } from '@ui/primitives';
import { WatchSubscriptionActionsFacade } from '../state/watch-subscription-actions.facade';

const PARK_GROUPS: readonly WatchEventGroup[] = [
  {
    key: 'practical',
    eventTypes: [
      'OpeningCalendarPublished', 'OpeningCalendarChanged', 'SeasonOpeningConfirmed',
      'SeasonClosingConfirmed', 'ParkTemporaryClosureConfirmed', 'ParkPermanentClosureConfirmed',
      'ParkReopeningConfirmed', 'ParkNameChanged', 'OperatorChanged', 'TicketPricePublishedOrChanged'
    ]
  },
  {
    key: 'attractions',
    eventTypes: [
      'AttractionAnnouncedOfficially', 'OpeningDateConfirmed', 'OpeningDateChanged', 'OpenedConfirmed',
      'TemporarilyClosedConfirmed', 'ReopenedConfirmed', 'PermanentClosureConfirmed', 'Renamed',
      'MajorRestrictionChanged', 'LocationOrCategoryCorrected'
    ]
  },
  {
    key: 'editorial',
    eventTypes: [
      'MajorDataCompletionImproved', 'HistoryPublished', 'MajorHistoryUpdate',
      'VerifiedSourceAdded', 'CorrectionAfterUserReport'
    ]
  }
];

const PARK_ITEM_GROUPS: readonly WatchEventGroup[] = [
  {
    key: 'lifecycle',
    eventTypes: [
      'AttractionAnnouncedOfficially', 'OpeningDateConfirmed', 'OpeningDateChanged', 'OpenedConfirmed',
      'TemporarilyClosedConfirmed', 'ReopenedConfirmed', 'PermanentClosureConfirmed', 'Renamed'
    ]
  },
  {
    key: 'details',
    eventTypes: ['MajorRestrictionChanged', 'LocationOrCategoryCorrected']
  },
  {
    key: 'editorial',
    eventTypes: ['HistoryPublished', 'MajorHistoryUpdate', 'VerifiedSourceAdded', 'CorrectionAfterUserReport']
  }
];

@Component({
  selector: 'app-watch-subscription-action',
  templateUrl: './watch-subscription-action.component.html',
  styleUrl: './watch-subscription-action.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [WatchSubscriptionActionsFacade],
  imports: [TranslateModule, UiButtonDirective, UiSurfaceDirective]
})
export class WatchSubscriptionActionComponent implements OnChanges {
  @Input({ required: true }) targetType: UserCollectionTargetType = 'Park';
  @Input({ required: true }) targetId: string = '';

  constructor(protected readonly facade: WatchSubscriptionActionsFacade) {
  }

  ngOnChanges(_changes: SimpleChanges): void {
    this.facade.configure(this.targetType, this.targetId, this.allEventTypes);
  }

  protected get groups(): readonly WatchEventGroup[] {
    return this.targetType === 'Park' ? PARK_GROUPS : PARK_ITEM_GROUPS;
  }

  protected get allEventTypes(): readonly FactualEventType[] {
    return this.groups.flatMap((group: WatchEventGroup): readonly FactualEventType[] => group.eventTypes);
  }

  protected groupLabelKey(group: WatchEventGroup): string {
    return `watchlists.groups.${group.key}.title`;
  }

  protected groupDescriptionKey(group: WatchEventGroup): string {
    return `watchlists.groups.${group.key}.description`;
  }
}
