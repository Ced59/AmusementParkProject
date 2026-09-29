import type { PassportProductEvent } from './passport-product-event.model';
import type { ShareProductEvent } from './share-product-event.model';

export type ProductAnalyticsFamily =
  | 'rank'
  | 'passport'
  | 'share'
  | 'fit'
  | 'watch'
  | 'trip'
  | 'history'
  | 'live';

export type ProductAnalyticsSurface =
  | 'methodology'
  | 'ranking-card'
  | 'park-page'
  | 'item-page'
  | 'passport'
  | 'profile';

export type ProductAnalyticsEvidenceLevel =
  | 'NoEvidence'
  | 'Insufficient'
  | 'Provisional'
  | 'Eligible'
  | 'Established'
  | 'StrongEvidence'
  | 'Excluded';

export type RankProductEvent =
  | { readonly type: 'ranking_methodology_opened'; readonly surface: ProductAnalyticsSurface }
  | {
      readonly type: 'ranking_evidence_details_opened';
      readonly evidenceLevel: ProductAnalyticsEvidenceLevel;
    }
  | {
      readonly type: 'provisional_rating_state_seen';
      readonly evidenceLevel: ProductAnalyticsEvidenceLevel;
      readonly surface: ProductAnalyticsSurface;
    }
  | {
      readonly type: 'ranking_data_issue_reported';
      readonly issueKind: 'incorrect-data' | 'missing-source' | 'duplicate-entry' | 'other';
    };

export type FitProductEvent =
  | { readonly type: 'park_fit_search_started' }
  | {
      readonly type: 'park_fit_search_completed';
      readonly resultBand: 'None' | 'One' | 'TwoToFour' | 'FiveOrMore';
      readonly unknownLevel: 'None' | 'Limited' | 'Significant';
      readonly durationBand:
        | 'UnderHalfSecond'
        | 'UnderOneAndHalfSeconds'
        | 'UnderThreeSeconds'
        | 'ThreeSecondsOrMore';
      readonly methodVersion: string;
    }
  | {
      readonly type: 'park_fit_search_failed';
      readonly failureKind: 'Validation' | 'RateLimited' | 'Technical';
      readonly durationBand:
        | 'UnderHalfSecond'
        | 'UnderOneAndHalfSeconds'
        | 'UnderThreeSeconds'
        | 'ThreeSecondsOrMore';
    }
  | {
      readonly type: 'park_fit_explanation_opened';
      readonly unknownLevel: 'None' | 'Limited' | 'Significant';
      readonly methodVersion: string;
    }
  | {
      readonly type: 'park_fit_comparison_opened';
      readonly comparisonSize: 'Two' | 'Three' | 'Four';
      readonly methodVersion: string;
    };

export type WatchProductEvent =
  | { readonly type: 'notification_center_opened' }
  | {
      readonly type: 'notification_source_opened' | 'misleading_alert_reported';
      readonly notificationKind: 'status-change' | 'wait-threshold' | 'reopening' | 'forecast';
    }
  | {
      readonly type: 'watch_subscription_removed';
      readonly subscriptionKind: 'live-alert' | 'watchlist';
    }
  | { readonly type: 'duplicate_delivery_prevented'; readonly deliveryChannel: 'email' };

export type TripProductEvent =
  | {
      readonly type: 'trip_created';
      readonly mode: 'solo' | 'group';
      readonly dayCountBucket: 'one' | 'two-to-three' | 'four-plus';
    }
  | {
      readonly type: 'trip_candidate_added';
      readonly source: 'search' | 'park-page' | 'recommendation';
    }
  | { readonly type: 'trip_invitation_accepted'; readonly role: 'owner' | 'participant' }
  | {
      readonly type: 'trip_preferences_recorded';
      readonly countBucket: 'one' | 'two-to-five' | 'six-plus';
    }
  | {
      readonly type: 'trip_conflict_opened';
      readonly conflictCountBucket: 'one' | 'two-to-five' | 'six-plus';
    }
  | {
      readonly type: 'trip_decision_recorded';
      readonly decisionKind: 'accepted' | 'rejected' | 'deferred';
    }
  | { readonly type: 'trip_export_requested'; readonly format: 'Json' | 'Csv' | 'Ics' }
  | {
      readonly type: 'trip_passport_transition_confirmed';
      readonly visitCountBucket: 'one' | 'two-to-five' | 'six-plus';
    };

export type HistoryProductEvent =
  | { readonly type: 'history_timeline_opened'; readonly coverageLevel: 'none' | 'partial' | 'complete' }
  | {
      readonly type: 'history_year_opened';
      readonly coverageLevel: 'none' | 'partial' | 'complete';
      readonly yearKind: 'current' | 'past';
    }
  | {
      readonly type: 'history_comparison_opened';
      readonly coverageLevel: 'none' | 'partial' | 'complete';
    }
  | {
      readonly type: 'history_source_opened';
      readonly sourceKind: 'official' | 'archive' | 'editorial';
    }
  | {
      readonly type: 'history_data_issue_reported';
      readonly issueKind: 'incorrect-data' | 'missing-period' | 'missing-source' | 'other';
    }
  | {
      readonly type: 'history_passport_context_opened';
      readonly verificationState: 'verified' | 'unverified' | 'mixed';
    };

export type LiveProductEvent =
  | {
      readonly type: 'live_status_seen';
      readonly freshnessState: 'Fresh' | 'Aging' | 'Stale' | 'Expired' | 'Unavailable';
      readonly operatingState:
        | 'Open'
        | 'Closed'
        | 'TemporarilyClosed'
        | 'Delayed'
        | 'Down'
        | 'WeatherClosed'
        | 'Maintenance'
        | 'OperatingWithLimitations'
        | 'Unknown'
        | 'NotOperatingToday'
        | 'Removed';
    }
  | {
      readonly type: 'live_refresh_requested';
      readonly outcome: 'refreshed' | 'unchanged' | 'unavailable' | 'failed';
    }
  | {
      readonly type: 'live_forecast_seen';
      readonly confidenceBand: 'low' | 'medium' | 'high';
      readonly methodVersion: string;
    }
  | { readonly type: 'live_forecast_method_opened'; readonly methodVersion: string }
  | {
      readonly type: 'live_alert_created';
      readonly thresholdBucket: 'under-15' | '15-to-29' | '30-to-59' | '60-plus';
    }
  | { readonly type: 'live_alert_removed' };

export type ProductAnalyticsEvent =
  | RankProductEvent
  | PassportProductEvent
  | ShareProductEvent
  | FitProductEvent
  | WatchProductEvent
  | TripProductEvent
  | HistoryProductEvent
  | LiveProductEvent;

export type ConsentedProductAnalyticsEvent = PassportProductEvent | ShareProductEvent;
