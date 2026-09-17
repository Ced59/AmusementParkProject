export const WATCH_SUBSCRIPTIONS_API_ENDPOINTS = {
  collection: 'me/watch-subscriptions',
  entry: (subscriptionId: string): string =>
    `me/watch-subscriptions/${encodeURIComponent(subscriptionId)}`,
  pause: (subscriptionId: string): string =>
    `me/watch-subscriptions/${encodeURIComponent(subscriptionId)}/pause`,
  resume: (subscriptionId: string): string =>
    `me/watch-subscriptions/${encodeURIComponent(subscriptionId)}/resume`
} as const;
