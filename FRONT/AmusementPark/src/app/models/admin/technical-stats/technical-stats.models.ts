export interface TechnicalStatsSnapshot {
  isAvailable: boolean;
  unavailableReason?: string | null;
  generatedAtUtc: string;
  startedAtUtc: string;
  uptimeSeconds: number;
  buildVersion: string;
  cache: TechnicalStatsCacheSummary;
  storage: TechnicalStatsStorageSummary;
  rendering: TechnicalStatsRenderingSummary;
  refresh: TechnicalStatsRefreshSummary;
  invalidation: TechnicalStatsInvalidationSummary;
  config: TechnicalStatsRuntimeConfig;
}

export interface TechnicalStatsCacheSummary {
  pageResponses: number;
  cacheablePageResponses: number;
  cacheHitResponses: number;
  hitRatePercent: number;
  robotPageResponses: number;
  robotCacheHitResponses: number;
  robotHitRatePercent: number;
  statuses: TechnicalStatsCount[];
  robotFamilies: TechnicalStatsRobotFamily[];
}

export interface TechnicalStatsStorageSummary {
  memoryEntries: number;
  memoryMaxEntries: number;
  diskEnabled: boolean;
  diskEntries: number;
  diskBytes: number;
  diskMaxBytes: number;
  diskWrites: number;
  technicalStatsPersistenceEntries: number;
  technicalStatsPersistenceBytes: number;
  technicalStatsPersistencePurgedBuckets: number;
  seoDocumentEntries: number;
  seoDocumentMaxEntries: number;
  seoDocumentRequests: number;
  seoDocumentHits: number;
  seoDocumentMisses: number;
  assetMisses: number;
}

export interface TechnicalStatsRenderingSummary {
  ssrRenderEnabled: boolean;
  renderOnCacheMiss: boolean;
  renderCriticalRoutesOnCacheMiss: boolean;
  activeRenders: number;
  queuedRenders: number;
  maxConcurrency: number;
  maxQueueEntries: number;
  totalRenders: number;
  averageRenderMilliseconds: number;
  maxRenderMilliseconds: number;
  slowRenders: number;
  slowRenderThresholdMilliseconds: number;
  queueFullRejections: number;
}

export interface TechnicalStatsRefreshSummary {
  enabled: boolean;
  pendingRefreshes: number;
  activeRefreshes: number;
  deduplicatedRefreshKeys: number;
  queuedRefreshes: number;
  succeededRefreshes: number;
  failedRefreshes: number;
  maxUrls: number;
  concurrency: number;
  delayMilliseconds: number;
  timeoutSeconds: number;
}

export interface TechnicalStatsInvalidationSummary {
  requests: number;
  allRequests: number;
  targetedRequests: number;
  clearedEntries: number;
  staleEntries: number;
  queuedRefreshes: number;
  lastInvalidationUtc: string | null;
}

export interface TechnicalStatsRuntimeConfig {
  pageCacheTtlSeconds: number;
  stalePageCacheSeconds: number;
  pageCacheMaxHtmlBytes: number;
  pageCacheBrowserCacheControl: string;
  csrFallbackCacheControl: string;
  seoDocumentBrowserCacheControl: string;
  technicalStatsPersistenceEnabled: boolean;
  technicalStatsPersistenceRetentionDays: number;
  technicalStatsPersistenceFlushIntervalSeconds: number;
  technicalStatsPersistenceLastFlushUtc: string | null;
  technicalStatsPersistenceLastCleanupUtc: string | null;
}

export interface UpdateTechnicalStatsSettingsRequest {
  persistenceRetentionDays: number;
}

export interface TechnicalStatsSettings {
  persistenceRetentionDays: number;
}

export interface TechnicalStatsCount {
  key: string;
  count: number;
  percent: number;
}

export interface TechnicalStatsRobotFamily {
  key: string;
  count: number;
  cacheHits: number;
  hitRatePercent: number;
}
