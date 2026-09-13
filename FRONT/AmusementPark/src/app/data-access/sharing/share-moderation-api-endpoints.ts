export const SHARE_MODERATION_API_ENDPOINTS = {
  submit: 'passport/shared/reports',
  adminReports: 'admin/share-moderation/reports',
  adminReview: (reportId: string): string =>
    `admin/share-moderation/reports/${encodeURIComponent(reportId)}`,
} as const;
