export interface TripNotificationState {
  enabled: boolean;
  version: number;
  unreadCount: number;
  hasMoreUnread: boolean;
}

export interface SetTripNotificationsRequest {
  enabled: boolean;
  expectedVersion: number;
}

export interface MarkTripNotificationsReadRequest {
  expectedVersion: number;
}
