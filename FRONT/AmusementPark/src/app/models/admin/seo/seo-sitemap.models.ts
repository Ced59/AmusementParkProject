import { CollectionResponse } from '@shared/models/contracts';

export type SeoSitemapGenerationStatus = 'Idle' | 'Running' | 'Succeeded' | 'Failed' | 'Skipped';

export interface SeoSitemapRuntime {
  status: SeoSitemapGenerationStatus;
  currentStep: string;
  progressPercentage: number;
  startedAtUtc: string | null;
  updatedAtUtc: string | null;
  message: string | null;
}

export interface SeoSitemapSectionStats {
  key: string;
  fileName: string;
  displayName: string;
  urlCount: number;
  lastModifiedUtc: string | null;
  publicUrl: string;
}

export interface SeoSitemapSettings {
  isIndexNowEnabled: boolean;
  submitToIndexNowAfterManualGeneration: boolean;
  submitToIndexNowAfterAutomaticGeneration: boolean;
  indexNowKey: string;
  indexNowKeyLocation: string;
  indexNowEndpoints: string[];
  updatedAtUtc: string;
}

export interface SeoSitemapOverview {
  runtime: SeoSitemapRuntime;
  lastGeneratedAtUtc: string | null;
  publicBaseUrl: string;
  totalUrlCount: number;
  sections: SeoSitemapSectionStats[];
  settings: SeoSitemapSettings;
  sitemapIndexUrl: string;
  robotsUrl: string;
  indexNowKeyFileUrl: string;
  publicSitemapUrls: string[];
}

export interface GenerateSeoSitemapRequest {
  submitToIndexNow: boolean;
}


export interface SeoSsrPrerenderProgress {
  status: SeoSitemapGenerationStatus;
  totalUrlCount: number;
  processedUrlCount: number;
  succeededUrlCount: number;
  failedUrlCount: number;
  currentUrl: string | null;
  errors: string[];
}

export interface SeoSsrPrerenderResult extends SeoSsrPrerenderProgress {
  startedAtUtc: string;
  completedAtUtc: string | null;
  durationMs: number;
}

export interface UpdateSeoSitemapSettingsRequest {
  isIndexNowEnabled: boolean;
  submitToIndexNowAfterManualGeneration: boolean;
  submitToIndexNowAfterAutomaticGeneration: boolean;
  indexNowKey: string | null;
  indexNowKeyLocation: string | null;
  indexNowEndpoints: string[];
}

export interface SeoIndexNowSubmission {
  wasRequested: boolean;
  isEnabled: boolean;
  isSuccess: boolean;
  submittedUrlCount: number;
  acceptedEndpoints: string[];
  errors: string[];
}

export interface SeoSitemapGenerationResult {
  id: string;
  startedAtUtc: string;
  completedAtUtc: string | null;
  durationMs: number;
  status: SeoSitemapGenerationStatus;
  trigger: string;
  totalUrlCount: number;
  sections: SeoSitemapSectionStats[];
  errors: string[];
  indexNow: SeoIndexNowSubmission;
}

export interface SeoSitemapGenerationHistory extends SeoSitemapGenerationResult {
  triggeredByUserId: string | null;
  triggeredByUserEmail: string | null;
}

export type SeoSitemapHistoryResponse = CollectionResponse<SeoSitemapGenerationHistory>;
