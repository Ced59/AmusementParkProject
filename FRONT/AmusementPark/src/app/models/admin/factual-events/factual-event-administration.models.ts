export type FactualChangeStatus = 'Draft' | 'Verified' | 'Published' | 'Corrected' | 'Retracted' | 'Expired';

export type FactualTargetType = 'Park' | 'ParkItem';

export type FactualDataConfidence = 'Low' | 'Medium' | 'High';

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

export interface FactualChangeTargetAdmin {
  readonly type: FactualTargetType;
  readonly name: string | null;
  readonly parentParkName: string | null;
}

export interface FactualFactValueAdmin {
  readonly kind: string;
  readonly canonicalValue: string;
  readonly unitCode: string | null;
}

export interface FactualSourceReferenceAdmin {
  readonly type: string;
  readonly publisherName: string;
  readonly title: string;
  readonly url: string;
  readonly publishedAtUtc: string;
}

export interface FactualChangeEventAdmin {
  readonly eventId: string;
  readonly type: FactualEventType;
  readonly definitionVersion: number;
  readonly target: FactualChangeTargetAdmin;
  readonly previousValue: FactualFactValueAdmin | null;
  readonly newValue: FactualFactValueAdmin | null;
  readonly source: FactualSourceReferenceAdmin;
  readonly confidence: FactualDataConfidence;
  readonly occurredAtUtc: string;
  readonly revision: number;
  readonly status: FactualChangeStatus;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
  readonly verifiedAtUtc: string | null;
  readonly publishedAtUtc: string | null;
  readonly terminalAtUtc: string | null;
  readonly supersededByEventId: string | null;
  readonly reasonCode: string | null;
  readonly version: number;
  readonly canBeDistributed: boolean;
}

export interface FactualChangeEventQuery {
  readonly page: number;
  readonly size: number;
  readonly status?: FactualChangeStatus;
  readonly targetType?: FactualTargetType;
  readonly eventType?: FactualEventType;
  readonly confidence?: FactualDataConfidence;
}

export interface FactualChangeEventMutationRequest {
  readonly expectedVersion: number;
}

export interface CorrectFactualChangeEventRequest extends FactualChangeEventMutationRequest {
  readonly supersedingEventId: string;
}

export interface RetractFactualChangeEventRequest extends FactualChangeEventMutationRequest {
  readonly reasonCode: string;
}

export type FactualEventAdminAction = 'verify' | 'publish' | 'correct' | 'retract';

export type FactualEventAdminActionError = 'conflict' | 'failure' | null;
