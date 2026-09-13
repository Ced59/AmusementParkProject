export const SHARE_PUBLICATIONS_API_ENDPOINTS = {
  preview: 'me/shares/preview',
  publish: 'me/shares/publish',
  visitSettings: (visitId: string): string => `me/passport/visits/${encodeURIComponent(visitId)}/share`,
  visitCandidates: (visitId: string, includeMissedItems: boolean): string =>
    `me/passport/visits/${encodeURIComponent(visitId)}/share/candidates?includeMissedItems=${includeMissedItems}`,
  sharedVisit: (shareId: string): string => `passport/shared/visits/${encodeURIComponent(shareId)}`,
  yearSettings: (year: number): string => `me/passport/years/${year}/share`,
  yearSelection: (year: number): string => `me/passport/years/${year}/share/selection`,
  sharedYear: (shareId: string): string => `passport/shared/years/${encodeURIComponent(shareId)}`,
  passportProfileSettings: 'me/passport/share',
  passportProfileSelection: 'me/passport/share/selection',
  sharedPassportProfile: (shareId: string): string => `passport/shared/profiles/${encodeURIComponent(shareId)}`,
  socialImage: (
    publicationType: 'visit' | 'year' | 'passport',
    shareId: string,
    publicationVersion: number,
    language: string
  ): string => `sharing/social-images/${publicationType}/${encodeURIComponent(shareId)}`
    + `/v${publicationVersion}/t1/${encodeURIComponent(language)}.png`
} as const;
