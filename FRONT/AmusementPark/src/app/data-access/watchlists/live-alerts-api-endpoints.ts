export const LIVE_ALERTS_API_ENDPOINTS = {
  collection: 'me/live-alerts',
  entry: (subscriptionId: string): string => `me/live-alerts/${encodeURIComponent(subscriptionId)}`,
  read: (notificationId: string): string =>
    `me/live-alerts/notifications/${encodeURIComponent(notificationId)}/read`,
  dismiss: (notificationId: string): string =>
    `me/live-alerts/notifications/${encodeURIComponent(notificationId)}/dismiss`
} as const;
