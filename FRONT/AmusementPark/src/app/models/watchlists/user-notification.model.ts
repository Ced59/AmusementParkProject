import { FactualEventType } from './watch-subscription.model';

export interface UserNotificationFactValue {
  kind: string;
  canonicalValue: string;
  unitCode: string | null;
}

export interface UserNotificationSource {
  type: string;
  publisherName: string | null;
  title: string | null;
  url: string | null;
  publishedAtUtc: string | null;
  verifiedAtUtc: string;
}

export interface UserNotificationTarget {
  type: 'Park' | 'ParkItem';
  targetId: string;
  parkId: string | null;
  name: string | null;
  parentParkName: string | null;
  mainImageId: string | null;
}

export interface UserNotification {
  notificationId: string;
  eventType: FactualEventType;
  status: 'Delivered' | 'Read';
  target: UserNotificationTarget;
  previousValue: UserNotificationFactValue | null;
  newValue: UserNotificationFactValue | null;
  source: UserNotificationSource;
  occurredAtUtc: string;
  deliveredAtUtc: string;
  readAtUtc: string | null;
  expiresAtUtc: string;
  version: number;
  canManageSubscription: boolean;
  subscriptionVersion: number | null;
}

export interface UserNotificationParkFilter {
  parkId: string;
  parkName: string;
}

export interface UserNotificationPage {
  items: UserNotification[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  unreadCount: number;
  retentionDays: number;
  parkFilters: UserNotificationParkFilter[];
}

export interface UserNotificationSearch {
  page: number;
  size: number;
  unreadOnly: boolean;
  parkId: string | null;
  eventType: FactualEventType | null;
}
