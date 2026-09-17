export type WatchPilotSignal =
  | 'AwaitingObservations'
  | 'NeedsAttention'
  | 'Monitor'
  | 'ReadyToExtend';

export interface WatchPilotMetricsQuery {
  readonly fromUtc?: string | null;
  readonly toUtc?: string | null;
}

export interface WatchPilotHealth {
  readonly duplicateRatePercent: number;
  readonly misleadingReportRatePercent: number;
  readonly emailFailureRatePercent: number;
  readonly averageDeliveryLatencyMinutes: number;
  readonly signal: WatchPilotSignal;
  readonly canExtendEventTypes: boolean;
  readonly providerFeedbackAvailable: boolean;
}

export interface WatchPilotDailyMetrics {
  readonly date: string;
  readonly notificationsDelivered: number;
  readonly digestsGenerated: number;
  readonly interactionCounts: Readonly<Record<string, number>>;
}

export interface WatchPilotMetricsResult {
  readonly generatedAtUtc: string;
  readonly fromUtc: string;
  readonly toUtc: string;
  readonly activeSubscriptions: number;
  readonly activeSubscriptionsByEventType: Readonly<Record<string, number>>;
  readonly eventsVerified: number;
  readonly eventsPublished: number;
  readonly eventsCorrected: number;
  readonly eventsRetracted: number;
  readonly notificationsDelivered: number;
  readonly duplicateNotifications: number;
  readonly notificationCenterOpens: number;
  readonly sourceOpens: number;
  readonly misleadingAlertReports: number;
  readonly subscriptionsRemoved: number;
  readonly digestsGenerated: number;
  readonly emailPending: number;
  readonly emailSucceeded: number;
  readonly emailFailed: number;
  readonly emailCancelled: number;
  readonly bounceCount: number | null;
  readonly complaintCount: number | null;
  readonly pendingOutboxEntries: number;
  readonly queueCountsByStatus: Readonly<Record<string, number>>;
  readonly health: WatchPilotHealth;
  readonly daily: readonly WatchPilotDailyMetrics[];
}
