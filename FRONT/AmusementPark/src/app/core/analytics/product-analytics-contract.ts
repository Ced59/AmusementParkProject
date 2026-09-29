import type {
  ProductAnalyticsEvent,
  ProductAnalyticsFamily
} from './product-analytics-event.model';

export const PRODUCT_ANALYTICS_SCHEMA_VERSION = 1;

interface ProductAnalyticsValueValidator {
  readonly accepts: (value: string) => boolean;
}

type ProductAnalyticsEventName = ProductAnalyticsEvent['type'];
type ProductAnalyticsEventFor<EventName extends ProductAnalyticsEventName> =
  ProductAnalyticsEvent extends infer Event
    ? Event extends { readonly type: infer CandidateName }
      ? EventName extends CandidateName
        ? Event
        : never
      : never
    : never;
type ProductAnalyticsPropertyName<EventName extends ProductAnalyticsEventName> = Exclude<
  keyof ProductAnalyticsEventFor<EventName>,
  'type'
>;

interface ProductAnalyticsEventContract<EventName extends ProductAnalyticsEventName> {
  readonly family: ProductAnalyticsFamily;
  readonly properties: {
    readonly [PropertyName in ProductAnalyticsPropertyName<EventName>]: ProductAnalyticsValueValidator;
  };
}

export interface ProductAnalyticsPayload {
  readonly schemaVersion: number;
  readonly family: ProductAnalyticsFamily;
  readonly channel: 'matomo-consented' | 'first-party' | 'technical' | 'reserved';
  readonly route: string;
  readonly category: string;
  readonly actionName: string;
  readonly action: ProductAnalyticsEventName;
  readonly label: string;
}

const passportSources = allowedValues('authenticated', 'anonymous-local');
const datePrecisions = allowedValues('Year', 'Month', 'Day');
const countBuckets = allowedValues('one', 'two-to-five', 'six-plus');
const recapTypes = allowedValues(
  'visit-recap',
  'year-recap',
  'passport-profile',
  'personal-ranking',
  'profile-comparison'
);
const rankingSurfaces = allowedValues(
  'methodology',
  'ranking-card',
  'park-page',
  'item-page',
  'passport',
  'profile'
);
const evidenceLevels = allowedValues(
  'NoEvidence',
  'Insufficient',
  'Provisional',
  'Eligible',
  'Established',
  'StrongEvidence',
  'Excluded'
);
const fitUnknownLevels = allowedValues('None', 'Limited', 'Significant');
const fitDurationBands = allowedValues(
  'UnderHalfSecond',
  'UnderOneAndHalfSeconds',
  'UnderThreeSeconds',
  'ThreeSecondsOrMore'
);
const methodVersion = matching(/^(?:park-fit|live-forecast)-\d{4}-\d{2}$/);
const historyCoverageLevels = allowedValues('none', 'partial', 'complete');

export const PRODUCT_ANALYTICS_EVENT_CONTRACTS = {
  ranking_methodology_opened: {
    family: 'rank',
    properties: { surface: rankingSurfaces }
  },
  ranking_evidence_details_opened: {
    family: 'rank',
    properties: { evidenceLevel: evidenceLevels }
  },
  provisional_rating_state_seen: {
    family: 'rank',
    properties: { evidenceLevel: evidenceLevels, surface: rankingSurfaces }
  },
  ranking_data_issue_reported: {
    family: 'rank',
    properties: {
      issueKind: allowedValues('incorrect-data', 'missing-source', 'duplicate-entry', 'other')
    }
  },
  passport_opened: {
    family: 'passport',
    properties: { source: passportSources }
  },
  visit_creation_started: {
    family: 'passport',
    properties: { source: passportSources, datePrecision: datePrecisions }
  },
  visit_created: {
    family: 'passport',
    properties: { source: passportSources, datePrecision: datePrecisions }
  },
  visit_completed: {
    family: 'passport',
    properties: { source: allowedValues('authenticated') }
  },
  visit_reopened: {
    family: 'passport',
    properties: { source: allowedValues('authenticated') }
  },
  second_visit_recorded: {
    family: 'passport',
    properties: { source: allowedValues('anonymous-local') }
  },
  ride_occurrence_added: {
    family: 'passport',
    properties: { source: passportSources, countBucket: countBuckets }
  },
  temporal_rating_added: {
    family: 'passport',
    properties: {
      source: allowedValues('authenticated'),
      targetType: allowedValues('park-visit', 'ride-occurrence')
    }
  },
  passport_statistics_opened: {
    family: 'passport',
    properties: {
      source: allowedValues('authenticated'),
      scope: allowedValues('global', 'park', 'item', 'year')
    }
  },
  passport_export_requested: {
    family: 'passport',
    properties: { source: passportSources, format: allowedValues('Json', 'Csv') }
  },
  passport_deletion_started: {
    family: 'passport',
    properties: { source: passportSources }
  },
  passport_deletion_completed: {
    family: 'passport',
    properties: { source: passportSources }
  },
  share_activation_started: { family: 'share', properties: { recapType: recapTypes } },
  share_preview_created: { family: 'share', properties: { recapType: recapTypes } },
  share_published: { family: 'share', properties: { recapType: recapTypes } },
  share_revoked: { family: 'share', properties: { recapType: recapTypes } },
  share_rotated: { family: 'share', properties: { recapType: recapTypes } },
  share_opened: { family: 'share', properties: { recapType: recapTypes } },
  share_cta_passport_started: { family: 'share', properties: { recapType: recapTypes } },
  share_render_failed: { family: 'share', properties: { recapType: recapTypes } },
  park_fit_search_started: { family: 'fit', properties: {} },
  park_fit_search_completed: {
    family: 'fit',
    properties: {
      resultBand: allowedValues('None', 'One', 'TwoToFour', 'FiveOrMore'),
      unknownLevel: fitUnknownLevels,
      durationBand: fitDurationBands,
      methodVersion
    }
  },
  park_fit_search_failed: {
    family: 'fit',
    properties: {
      failureKind: allowedValues('Validation', 'RateLimited', 'Technical'),
      durationBand: fitDurationBands
    }
  },
  park_fit_explanation_opened: {
    family: 'fit',
    properties: { unknownLevel: fitUnknownLevels, methodVersion }
  },
  park_fit_comparison_opened: {
    family: 'fit',
    properties: { comparisonSize: allowedValues('Two', 'Three', 'Four'), methodVersion }
  },
  notification_center_opened: { family: 'watch', properties: {} },
  notification_source_opened: {
    family: 'watch',
    properties: {
      notificationKind: allowedValues('status-change', 'wait-threshold', 'reopening', 'forecast')
    }
  },
  misleading_alert_reported: {
    family: 'watch',
    properties: {
      notificationKind: allowedValues('status-change', 'wait-threshold', 'reopening', 'forecast')
    }
  },
  watch_subscription_removed: {
    family: 'watch',
    properties: { subscriptionKind: allowedValues('live-alert', 'watchlist') }
  },
  duplicate_delivery_prevented: {
    family: 'watch',
    properties: { deliveryChannel: allowedValues('email') }
  },
  trip_created: {
    family: 'trip',
    properties: {
      mode: allowedValues('solo', 'group'),
      dayCountBucket: allowedValues('one', 'two-to-three', 'four-plus')
    }
  },
  trip_candidate_added: {
    family: 'trip',
    properties: { source: allowedValues('search', 'park-page', 'recommendation') }
  },
  trip_invitation_accepted: {
    family: 'trip',
    properties: { role: allowedValues('owner', 'participant') }
  },
  trip_preferences_recorded: {
    family: 'trip',
    properties: { countBucket: countBuckets }
  },
  trip_conflict_opened: {
    family: 'trip',
    properties: { conflictCountBucket: countBuckets }
  },
  trip_decision_recorded: {
    family: 'trip',
    properties: { decisionKind: allowedValues('accepted', 'rejected', 'deferred') }
  },
  trip_export_requested: {
    family: 'trip',
    properties: { format: allowedValues('Json', 'Csv', 'Ics') }
  },
  trip_passport_transition_confirmed: {
    family: 'trip',
    properties: { visitCountBucket: countBuckets }
  },
  history_timeline_opened: {
    family: 'history',
    properties: { coverageLevel: historyCoverageLevels }
  },
  history_year_opened: {
    family: 'history',
    properties: {
      coverageLevel: historyCoverageLevels,
      yearKind: allowedValues('current', 'past')
    }
  },
  history_comparison_opened: {
    family: 'history',
    properties: { coverageLevel: historyCoverageLevels }
  },
  history_source_opened: {
    family: 'history',
    properties: { sourceKind: allowedValues('official', 'archive', 'editorial') }
  },
  history_data_issue_reported: {
    family: 'history',
    properties: {
      issueKind: allowedValues('incorrect-data', 'missing-period', 'missing-source', 'other')
    }
  },
  history_passport_context_opened: {
    family: 'history',
    properties: { verificationState: allowedValues('verified', 'unverified', 'mixed') }
  },
  live_status_seen: {
    family: 'live',
    properties: {
      freshnessState: allowedValues('Fresh', 'Aging', 'Stale', 'Expired', 'Unavailable'),
      operatingState: allowedValues(
        'Open',
        'Closed',
        'TemporarilyClosed',
        'Delayed',
        'Down',
        'WeatherClosed',
        'Maintenance',
        'OperatingWithLimitations',
        'Unknown',
        'NotOperatingToday',
        'Removed'
      )
    }
  },
  live_refresh_requested: {
    family: 'live',
    properties: { outcome: allowedValues('refreshed', 'unchanged', 'unavailable', 'failed') }
  },
  live_forecast_seen: {
    family: 'live',
    properties: {
      confidenceBand: allowedValues('low', 'medium', 'high'),
      methodVersion
    }
  },
  live_forecast_method_opened: {
    family: 'live',
    properties: { methodVersion }
  },
  live_alert_created: {
    family: 'live',
    properties: {
      thresholdBucket: allowedValues('under-15', '15-to-29', '30-to-59', '60-plus')
    }
  },
  live_alert_removed: { family: 'live', properties: {} }
} satisfies {
  readonly [EventName in ProductAnalyticsEventName]: ProductAnalyticsEventContract<EventName>;
};

const familyPresentation: Readonly<
  Record<
    ProductAnalyticsFamily,
    {
      readonly route: string;
      readonly category: string;
      readonly actionName: string;
      readonly channel: 'matomo-consented' | 'first-party' | 'technical' | 'reserved';
    }
  >
> = {
  rank: {
    route: 'rank', category: 'Ranking', actionName: 'Ranking product event', channel: 'reserved'
  },
  passport: {
    route: 'passport',
    category: 'Passport',
    actionName: 'Passport product event',
    channel: 'matomo-consented'
  },
  share: {
    route: 'share',
    category: 'Share',
    actionName: 'Share product event',
    channel: 'matomo-consented'
  },
  fit: {
    route: 'fit', category: 'Park fit', actionName: 'Park fit product event', channel: 'first-party'
  },
  watch: {
    route: 'watch', category: 'Watch', actionName: 'Watch product event', channel: 'first-party'
  },
  trip: { route: 'trip', category: 'Trip', actionName: 'Trip product event', channel: 'reserved' },
  history: {
    route: 'history', category: 'History', actionName: 'History product event', channel: 'reserved'
  },
  live: { route: 'live', category: 'Live', actionName: 'Live product event', channel: 'reserved' }
};

export function serializeProductAnalyticsEvent(event: unknown): ProductAnalyticsPayload | null {
  if (!isRecord(event) || typeof event['type'] !== 'string') {
    return null;
  }

  const eventName: string = event['type'];
  if (!Object.prototype.hasOwnProperty.call(PRODUCT_ANALYTICS_EVENT_CONTRACTS, eventName)) {
    return null;
  }

  const contract = PRODUCT_ANALYTICS_EVENT_CONTRACTS[
    eventName as ProductAnalyticsEventName
  ] as ProductAnalyticsEventContract<ProductAnalyticsEventName>;
  const expectedPropertyNames: string[] = Object.keys(contract.properties);
  const receivedPropertyNames: string[] = Object.keys(event).filter(
    (propertyName: string) => propertyName !== 'type'
  );
  if (
    expectedPropertyNames.length !== receivedPropertyNames.length
    || receivedPropertyNames.some(
      (propertyName: string) => !Object.prototype.hasOwnProperty.call(contract.properties, propertyName)
    )
  ) {
    return null;
  }

  const serializedProperties: string[] = [];
  for (const propertyName of expectedPropertyNames) {
    const value: unknown = event[propertyName];
    const validator: ProductAnalyticsValueValidator = contract.properties[
      propertyName as ProductAnalyticsPropertyName<ProductAnalyticsEventName>
    ];
    if (typeof value !== 'string' || !validator.accepts(value)) {
      return null;
    }

    serializedProperties.push(`${toKebabToken(propertyName)}=${toKebabToken(value)}`);
  }

  const presentation = familyPresentation[contract.family];
  return {
    schemaVersion: PRODUCT_ANALYTICS_SCHEMA_VERSION,
    family: contract.family,
    channel: eventName === 'duplicate_delivery_prevented' ? 'technical' : presentation.channel,
    route: presentation.route,
    category: presentation.category,
    actionName: presentation.actionName,
    action: eventName as ProductAnalyticsEventName,
    label: [`schema-version=${PRODUCT_ANALYTICS_SCHEMA_VERSION}`, ...serializedProperties].join(';')
  };
}

function allowedValues(...values: readonly string[]): ProductAnalyticsValueValidator {
  const allowed: ReadonlySet<string> = new Set(values);
  return { accepts: (value: string): boolean => allowed.has(value) };
}

function matching(pattern: RegExp): ProductAnalyticsValueValidator {
  return { accepts: (value: string): boolean => pattern.test(value) };
}

function isRecord(value: unknown): value is Readonly<Record<string, unknown>> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function toKebabToken(value: string): string {
  return value
    .replace(/([a-z0-9])([A-Z])/g, '$1-$2')
    .replace(/[_\s]+/g, '-')
    .toLowerCase();
}
