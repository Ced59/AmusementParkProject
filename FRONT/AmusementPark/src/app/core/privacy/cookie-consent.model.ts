export type CookieConsentDecision = 'accepted' | 'refused';

export interface StoredCookieConsent {
  readonly decision: CookieConsentDecision;
  readonly decidedAt: string;
  readonly version: number;
}
