export const PROFILE_COMPARISON_INVITATIONS_API_ENDPOINTS = {
  create: 'me/comparisons/invitations',
  preview: (token: string): string =>
    `me/comparisons/invitations/${encodeURIComponent(token)}/preview`,
  accept: (token: string): string =>
    `me/comparisons/invitations/${encodeURIComponent(token)}/accept`
} as const;
