export const USER_NOTIFICATIONS_API_ENDPOINTS = {
  collection: 'me/notifications',
  read: (notificationId: string): string =>
    `me/notifications/${encodeURIComponent(notificationId)}/read`,
  dismiss: (notificationId: string): string =>
    `me/notifications/${encodeURIComponent(notificationId)}/dismiss`,
  subscription: (notificationId: string): string =>
    `me/notifications/${encodeURIComponent(notificationId)}/subscription`,
  readAll: 'me/notifications/read-all',
  pilotInteractions: 'me/notifications/pilot-interactions'
} as const;
