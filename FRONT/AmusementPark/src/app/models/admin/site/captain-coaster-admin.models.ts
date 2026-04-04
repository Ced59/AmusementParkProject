/**
 * Modèles conservés pour rétrocompatibilité.
 * Les nouveaux modèles sont dans models/admin/data/data-management.models.ts
 */
export interface CaptainCoasterSettingsResponse {
  source: string;
  baseUrl: string;
  isEnabled: boolean;
  apiKeyMasked: string;
  lastSuccessfulSyncUtc?: string | null;
}
