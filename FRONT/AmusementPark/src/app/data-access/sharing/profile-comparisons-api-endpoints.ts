export const PROFILE_COMPARISONS_API_ENDPOINTS = {
  shared: (shareId: string): string =>
    `passport/shared/comparisons/${encodeURIComponent(shareId)}`,
  mine: 'me/profile-comparisons',
  revoke: (shareId: string): string =>
    `me/profile-comparisons/${encodeURIComponent(shareId)}`,
} as const;
