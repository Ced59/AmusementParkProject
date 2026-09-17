export interface NotificationEmailPreference {
  emailDigestEnabled: boolean;
  emailAvailable: boolean;
  maskedEmail: string | null;
  consentTextVersion: string;
  consentGrantedAtUtc: string | null;
  revokedAtUtc: string | null;
  version: number | null;
}

export interface NotificationEmailPreferenceUpdate {
  emailDigestEnabled: boolean;
  consentAccepted: boolean;
  consentLocale: string;
  expectedVersion: number | null;
}
