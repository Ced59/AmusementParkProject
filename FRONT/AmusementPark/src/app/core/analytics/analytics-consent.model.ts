export type AnalyticsConsentDecision = 'accepted' | 'refused';

export interface StoredAnalyticsConsent {
  readonly decision: AnalyticsConsentDecision;
  readonly decidedAt: string;
}
