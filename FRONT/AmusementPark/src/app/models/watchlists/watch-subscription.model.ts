import { UserCollectionTargetType } from './user-collection-entry.model';

export type FactualEventType =
  | 'OpeningCalendarPublished'
  | 'OpeningCalendarChanged'
  | 'SeasonOpeningConfirmed'
  | 'SeasonClosingConfirmed'
  | 'ParkTemporaryClosureConfirmed'
  | 'ParkPermanentClosureConfirmed'
  | 'ParkReopeningConfirmed'
  | 'ParkNameChanged'
  | 'OperatorChanged'
  | 'TicketPricePublishedOrChanged'
  | 'MajorDataCompletionImproved'
  | 'AttractionAnnouncedOfficially'
  | 'OpeningDateConfirmed'
  | 'OpeningDateChanged'
  | 'OpenedConfirmed'
  | 'TemporarilyClosedConfirmed'
  | 'ReopenedConfirmed'
  | 'PermanentClosureConfirmed'
  | 'Renamed'
  | 'MajorRestrictionChanged'
  | 'LocationOrCategoryCorrected'
  | 'HistoryPublished'
  | 'MajorHistoryUpdate'
  | 'VerifiedSourceAdded'
  | 'CorrectionAfterUserReport';

export interface WatchSubscription {
  subscriptionId: string;
  targetType: UserCollectionTargetType;
  targetId: string;
  targetName: string | null;
  parentParkId: string | null;
  parentParkName: string | null;
  mainImageId: string | null;
  eventTypes: FactualEventType[];
  frequency: 'WebOnly';
  channels: string[];
  isPaused: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: number;
}

export interface WatchSubscriptionWriteRequest {
  targetType: UserCollectionTargetType;
  targetId: string;
  eventTypes: FactualEventType[];
  frequency: 'WebOnly';
  channels: string[];
}

export interface WatchSubscriptionUpdateRequest {
  eventTypes: FactualEventType[];
  frequency: 'WebOnly';
  channels: string[];
  expectedVersion: number;
}

export interface WatchEventGroup {
  key: 'practical' | 'attractions' | 'editorial' | 'lifecycle' | 'details';
  eventTypes: readonly FactualEventType[];
}
