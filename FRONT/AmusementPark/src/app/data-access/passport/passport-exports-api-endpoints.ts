export const PASSPORT_EXPORTS_API_ENDPOINTS = {
  request: 'me/passport/exports',
  status: (exportId: string): string => `me/passport/exports/${encodeURIComponent(exportId)}`,
  download: (exportId: string): string => `me/passport/exports/${encodeURIComponent(exportId)}?download=true`
} as const;
