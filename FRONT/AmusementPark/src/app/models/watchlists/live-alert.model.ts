import { PublicLiveOperationalStatus } from '../live-data/public-live.models';

export type LiveAlertType = 'Reopened' | 'WaitBelow' | 'WaitAbove' | 'Degraded';
export type LiveAlertNotificationStatus = 'Delivered' | 'Read' | 'Dismissed';

export interface LiveAlertSubscription {
  readonly subscriptionId: string;
  readonly targetId: string;
  readonly parkId: string;
  readonly targetName: string | null;
  readonly parkName: string | null;
  readonly mainImageId: string | null;
  readonly type: LiveAlertType;
  readonly thresholdMinutes: number | null;
  readonly createdAtUtc: string;
  readonly expiresAtUtc: string;
  readonly lastObservedAtUtc: string | null;
  readonly lastStatus: PublicLiveOperationalStatus | null;
  readonly lastWaitMinutes: number | null;
  readonly cooldownMinutes: number;
  readonly hysteresisMinutes: number;
  readonly version: number;
}

export interface LiveAlertNotification {
  readonly notificationId: string;
  readonly subscriptionId: string;
  readonly targetId: string;
  readonly parkId: string;
  readonly targetName: string | null;
  readonly parkName: string | null;
  readonly mainImageId: string | null;
  readonly type: LiveAlertType;
  readonly thresholdMinutes: number | null;
  readonly previousStatus: PublicLiveOperationalStatus | null;
  readonly currentStatus: PublicLiveOperationalStatus;
  readonly previousWaitMinutes: number | null;
  readonly currentWaitMinutes: number | null;
  readonly sourceId: string;
  readonly sourceName: string | null;
  readonly attributionText: string | null;
  readonly attributionUrl: string | null;
  readonly observedAtUtc: string;
  readonly deliveredAtUtc: string;
  readonly ageSecondsAtDelivery: number;
  readonly status: LiveAlertNotificationStatus;
  readonly readAtUtc: string | null;
  readonly expiresAtUtc: string;
  readonly version: number;
}

export interface LiveAlertDashboard {
  readonly subscriptions: readonly LiveAlertSubscription[];
  readonly notifications: readonly LiveAlertNotification[];
  readonly unreadCount: number;
  readonly retentionDays: number;
}

export interface CreateLiveAlertRequest {
  readonly targetId: string;
  readonly type: LiveAlertType;
  readonly thresholdMinutes: number | null;
  readonly durationMinutes: number;
}
