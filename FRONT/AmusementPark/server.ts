import { APP_BASE_HREF } from '@angular/common';
import { CommonEngine } from '@angular/ssr/node';
import express, { NextFunction, Request, Response } from 'express';
import http from 'node:http';
import https from 'node:https';
import { dirname, join, resolve } from 'node:path';
import { Buffer } from 'node:buffer';
import { createHash } from 'node:crypto';
import { existsSync, mkdirSync, readdirSync, readFileSync, renameSync, statSync, unlinkSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import AppServerModule from './src/main.server';
import { SSR_RESPONSE } from './src/app/core/ssr/ssr-response.token';
import { isApiHeaderHiddenFromPublicProxy } from './src/app/core/ssr/public-api-header-policy';
import { resolveSsrRouteStatusCode, shouldApplyNoindexFollowHeader } from './src/app/core/ssr/ssr-route-status.helpers';
import { optimizeHtmlForRobotNoJs, RobotHtmlOptimizationResult } from './src/app/core/ssr/robot-html-optimizer';
import { buildCanonicalVideoRouteRedirectPath } from './src/app/core/seo/legacy-video-route.helpers';
import { siteVersion } from './src/environments/version.generated';

const defaultApiInternalOrigin = 'http://api:8080';
const apiInternalOrigin = normalizeOrigin(process.env['SSR_API_INTERNAL_URL'] ?? defaultApiInternalOrigin);
const currentBuildVersion = (process.env['APP_VERSION'] ?? siteVersion).trim() || siteVersion;
const cspReportOnly = (process.env['SSR_CSP_REPORT_ONLY'] ?? 'true').toLowerCase() !== 'false';
const cspEnabled = (process.env['SSR_CSP_ENABLED'] ?? 'true').toLowerCase() !== 'false';
const ssrAllowedHosts = splitConfiguredValues(process.env['SSR_ALLOWED_HOSTS'] ?? 'localhost;127.0.0.1;amusement.localhost;front');
const forceHttps = (process.env['SSR_FORCE_HTTPS'] ?? 'false').toLowerCase() === 'true';
const ssrRenderEnabled = (process.env['SSR_RENDER_ENABLED'] ?? 'true').toLowerCase() !== 'false';
const renderOnCacheMiss = (process.env['SSR_RENDER_ON_CACHE_MISS'] ?? 'false').toLowerCase() === 'true';
const renderCriticalRoutesOnCacheMiss = (process.env['SSR_RENDER_CRITICAL_ROUTES_ON_CACHE_MISS'] ?? 'true').toLowerCase() !== 'false';
const robotNoJsHtmlEnabled = (process.env['SSR_ROBOT_NO_JS_HTML_ENABLED'] ?? 'true').toLowerCase() !== 'false';
const allowLocalCspSources = (process.env['SSR_CSP_ALLOW_LOCAL_DEV_SOURCES'] ?? 'false').toLowerCase() === 'true';
const pageCacheTtlSeconds = Math.max(0, Number(process.env['SSR_PAGE_CACHE_SECONDS'] ?? 86400));
const pageCacheMaxEntries = Math.max(0, Number(process.env['SSR_PAGE_CACHE_MAX_ENTRIES'] ?? 2000));
const pageCache = new Map<string, PageCacheEntry>();
const pageCacheAllowAuthenticatedPublicHtml = (process.env['SSR_PUBLIC_PAGE_CACHE_ALLOW_AUTH_COOKIES'] ?? 'true').toLowerCase() !== 'false';
const diskPageCacheEnabled = (process.env['SSR_DISK_PAGE_CACHE_ENABLED'] ?? 'true').toLowerCase() !== 'false';
const diskPageCacheDirectory = process.env['SSR_DISK_PAGE_CACHE_DIR'] ?? '/tmp/amusementpark-ssr-page-cache';
const diskPageCacheMaxBytes = Math.max(0, Number(process.env['SSR_DISK_PAGE_CACHE_MAX_BYTES'] ?? 4 * 1024 * 1024 * 1024));
const diskPageCacheBudgetCheckEveryWrites = Math.max(1, Number(process.env['SSR_DISK_PAGE_CACHE_BUDGET_CHECK_EVERY_WRITES'] ?? 100));
const pageCacheMaxHtmlBytes = Math.max(0, Number(process.env['SSR_PAGE_CACHE_MAX_HTML_BYTES'] ?? 2 * 1024 * 1024));
const seoDocumentCacheTtlSeconds = Number(process.env['SSR_SEO_DOCUMENT_CACHE_SECONDS'] ?? 0);
const seoDocumentCacheMaxEntries = Math.max(0, Number(process.env['SSR_SEO_DOCUMENT_CACHE_MAX_ENTRIES'] ?? 128));
const seoDocumentCache = new Map<string, SeoDocumentCacheEntry>();
const pendingSeoDocumentCacheRequests = new Map<string, Array<SeoDocumentCacheCallback>>();
const internalSsrHeaderName = 'X-AmusementPark-Internal-SSR';
const renderMaxConcurrency = Math.max(1, Number(process.env['SSR_RENDER_MAX_CONCURRENCY'] ?? 1));
const renderQueueMaxEntries = Math.max(0, Number(process.env['SSR_RENDER_QUEUE_MAX_ENTRIES'] ?? 8));
const slowRenderThresholdMilliseconds = Math.max(0, Number(process.env['SSR_SLOW_RENDER_THRESHOLD_MILLISECONDS'] ?? 3000));
const renderQueueWarningThreshold = Math.max(1, Number(process.env['SSR_RENDER_QUEUE_WARNING_THRESHOLD'] ?? Math.max(1, Math.floor(Math.max(1, renderQueueMaxEntries) * 0.75))));
const assetMissLogSampleRate = Math.max(0, Number(process.env['SSR_ASSET_MISS_LOG_SAMPLE_RATE'] ?? 25));
const csrFallbackLogSampleRate = Math.max(0, Number(process.env['SSR_CSR_FALLBACK_LOG_SAMPLE_RATE'] ?? 100));
const csrFallbackCacheControl = process.env['SSR_CSR_FALLBACK_CACHE_CONTROL'] ?? 'public, max-age=60, stale-while-revalidate=300';
const pageCacheBrowserCacheControl = process.env['SSR_PAGE_CACHE_BROWSER_CACHE_CONTROL'] ?? 'no-cache, max-age=0, must-revalidate';
const seoDocumentBrowserCacheControl = process.env['SSR_SEO_DOCUMENT_BROWSER_CACHE_CONTROL'] ?? 'no-cache, max-age=0, must-revalidate';
const immutableBuildAssetCacheControl = 'public, max-age=31536000, immutable';
const revalidatedStaticAssetCacheControl = 'no-cache, max-age=0, must-revalidate';
const cacheInvalidationToken = process.env['SSR_CACHE_INVALIDATION_TOKEN'] ?? '';
const stalePageCacheSeconds = Math.max(0, Number(process.env['SSR_STALE_PAGE_CACHE_SECONDS'] ?? 600));
const targetedRefreshEnabled = (process.env['SSR_TARGETED_REFRESH_ENABLED'] ?? 'true').toLowerCase() !== 'false';
const targetedRefreshMaxUrls = Math.max(0, Number(process.env['SSR_TARGETED_REFRESH_MAX_URLS'] ?? 24));
const targetedRefreshConcurrency = Math.max(1, Number(process.env['SSR_TARGETED_REFRESH_CONCURRENCY'] ?? 1));
const targetedRefreshDelayMilliseconds = Math.max(0, Number(process.env['SSR_TARGETED_REFRESH_DELAY_MILLISECONDS'] ?? 1500));
const targetedRefreshTimeoutSeconds = Math.max(1, Number(process.env['SSR_TARGETED_REFRESH_TIMEOUT_SECONDS'] ?? 45));
const targetedInvalidationDebounceMilliseconds = Math.max(0, Number(process.env['SSR_TARGETED_INVALIDATION_DEBOUNCE_MILLISECONDS'] ?? 250));
const technicalStatsPersistenceEnabled = (process.env['SSR_TECHNICAL_STATS_PERSISTENCE_ENABLED'] ?? 'true').toLowerCase() !== 'false';
const technicalStatsPersistenceDirectory = process.env['SSR_TECHNICAL_STATS_PERSISTENCE_DIR'] ?? join(diskPageCacheDirectory, 'technical-stats');
const technicalStatsSettingsFilePath = join(technicalStatsPersistenceDirectory, 'settings.json');
const technicalStatsMinRetentionDays = 1;
const technicalStatsMaxRetentionDays = 365;
const technicalStatsDefaultRetentionDays = normalizeInteger(process.env['SSR_TECHNICAL_STATS_RETENTION_DAYS'], 15, technicalStatsMinRetentionDays, technicalStatsMaxRetentionDays);
const technicalStatsPersistenceFlushIntervalSeconds = normalizeInteger(process.env['SSR_TECHNICAL_STATS_FLUSH_INTERVAL_SECONDS'], 60, 5, 3600);
let technicalStatsStartedAtUtc = new Date();
let technicalStatsRetentionDays = technicalStatsDefaultRetentionDays;
let technicalStatsPersistenceLastFlushUtc: string | null = null;
let technicalStatsPersistenceLastCleanupUtc: string | null = null;
let technicalStatsPersistencePurgedBuckets = 0;
let technicalStatsPersistenceEntries = 0;
let technicalStatsPersistenceBytes = 0;
let technicalStatsPersistenceTimerStarted = false;
let activeRenderCount = 0;
let assetMissCount = 0;
let csrFallbackCount = 0;
let cachedCsrShellHtml: string | null = null;
let diskPageCacheWriteCount = 0;
const pendingRenderQueue: Array<() => void> = [];
const pendingTargetedRefreshQueue: string[] = [];
const queuedTargetedRefreshKeys = new Set<string>();
let activeTargetedRefreshCount = 0;
let pendingTargetedInvalidationRequest: NormalizedCacheInvalidationRequest | null = null;
let pendingTargetedInvalidationTimer: ReturnType<typeof setTimeout> | null = null;
let targetedInvalidationInProgress = false;

type SsrPageResponseStatus =
  'HIT'
  | 'MISS'
  | 'WARMED'
  | 'WARMUP-HIT'
  | 'STALE'
  | 'WARMUP-STALE'
  | 'SSR-UNCACHED'
  | 'CSR-CACHE-MISS-FALLBACK'
  | 'CSR-OVERLOAD-FALLBACK'
  | 'CSR-WARMUP-SKIPPED';

interface TechnicalStatsCounters {
  pageResponses: number;
  cacheablePageResponses: number;
  cacheHitResponses: number;
  robotPageResponses: number;
  robotCacheHitResponses: number;
  totalRenders: number;
  totalRenderMilliseconds: number;
  maxRenderMilliseconds: number;
  slowRenders: number;
  renderQueueFullRejections: number;
  targetedRefreshQueued: number;
  targetedRefreshSucceeded: number;
  targetedRefreshFailed: number;
  invalidationRequests: number;
  invalidationAllRequests: number;
  invalidationTargetedRequests: number;
  invalidationClearedEntries: number;
  invalidationStaleEntries: number;
  invalidationQueuedRefreshes: number;
  lastInvalidationUtc: string | null;
  seoDocumentRequests: number;
  seoDocumentHits: number;
  seoDocumentMisses: number;
}

interface TechnicalStatsCount {
  readonly key: string;
  readonly count: number;
  readonly percent: number;
}

interface TechnicalStatsRobotFamily {
  readonly key: string;
  readonly count: number;
  readonly cacheHits: number;
  readonly hitRatePercent: number;
}

interface DiskPageCacheStats {
  readonly entries: number;
  readonly bytes: number;
}

interface TechnicalStatsPersistentBucket {
  readonly version: 1;
  readonly date: string;
  readonly startedAtUtc: string;
  readonly updatedAtUtc: string;
  readonly counters: TechnicalStatsCounters;
  readonly pageResponseStatusCounts: Record<string, number>;
  readonly robotFamilyCounts: Record<string, number>;
  readonly robotFamilyCacheHitCounts: Record<string, number>;
}

interface TechnicalStatsPersistenceSettings {
  readonly retentionDays: number;
  readonly updatedAtUtc: string;
}

const technicalStatsCounters: TechnicalStatsCounters = {
  pageResponses: 0,
  cacheablePageResponses: 0,
  cacheHitResponses: 0,
  robotPageResponses: 0,
  robotCacheHitResponses: 0,
  totalRenders: 0,
  totalRenderMilliseconds: 0,
  maxRenderMilliseconds: 0,
  slowRenders: 0,
  renderQueueFullRejections: 0,
  targetedRefreshQueued: 0,
  targetedRefreshSucceeded: 0,
  targetedRefreshFailed: 0,
  invalidationRequests: 0,
  invalidationAllRequests: 0,
  invalidationTargetedRequests: 0,
  invalidationClearedEntries: 0,
  invalidationStaleEntries: 0,
  invalidationQueuedRefreshes: 0,
  lastInvalidationUtc: null,
  seoDocumentRequests: 0,
  seoDocumentHits: 0,
  seoDocumentMisses: 0
};

const pageResponseStatusCounts = new Map<SsrPageResponseStatus, number>();
const robotFamilyCounts = new Map<string, number>();
const robotFamilyCacheHitCounts = new Map<string, number>();
let lastPersistedTechnicalStatsCounters = cloneTechnicalStatsCounters(technicalStatsCounters);
let lastPersistedPageResponseStatusCounts = cloneNumberMap(pageResponseStatusCounts);
let lastPersistedRobotFamilyCounts = cloneNumberMap(robotFamilyCounts);
let lastPersistedRobotFamilyCacheHitCounts = cloneNumberMap(robotFamilyCacheHitCounts);

initializeTechnicalStatsPersistence();

interface PageCacheEntry {
  readonly buildVersion?: string;
  readonly cacheKey?: string;
  readonly statusCode: number;
  readonly html: string;
  readonly expiresAt: number;
  readonly staleUntil?: number;
}

interface CacheInvalidationRequest {
  readonly all?: boolean;
  readonly paths?: string[];
  readonly prefixes?: string[];
  readonly includeSeoDocuments?: boolean;
  readonly allowStale?: boolean;
  readonly refresh?: boolean;
}

interface NormalizedCacheInvalidationRequest {
  readonly all: boolean;
  readonly paths: string[];
  readonly prefixes: string[];
  readonly includeSeoDocuments: boolean;
  readonly allowStale: boolean;
  readonly refresh: boolean;
}

interface CacheInvalidationResult {
  readonly cleared: number;
  readonly pageMemoryEntries: number;
  readonly pageDiskEntries: number;
  readonly seoDocumentEntries: number;
  readonly stalePageEntries: number;
  readonly queuedRefreshes: number;
  readonly all: boolean;
  readonly deferred?: boolean;
}

interface SeoDocumentCacheEntry {
  readonly statusCode: number;
  readonly headers: Record<string, string | string[]>;
  readonly body: Buffer;
  readonly expiresAt: number | null;
}

type SeoDocumentCacheCallback = (error: Error | null, entry: SeoDocumentCacheEntry | null) => void;

class SsrRenderQueueFullError extends Error {
  constructor() {
    super('SSR render queue is full.');
    this.name = 'SsrRenderQueueFullError';
  }
}

export function app(): express.Express {
  const server = express();
  const serverDistFolder = dirname(fileURLToPath(import.meta.url));
  const browserDistFolder = resolveBrowserDistFolder(serverDistFolder);
  const indexHtml = resolveIndexServerHtml(serverDistFolder, browserDistFolder);
  const csrIndexHtml = resolveCsrIndexHtml(serverDistFolder, browserDistFolder);

  const commonEngine = new CommonEngine({ allowedHosts: ssrAllowedHosts });

  server.disable('x-powered-by');
  server.set('trust proxy', true);
  server.set('view engine', 'html');
  server.set('views', browserDistFolder);

  // Must stay before HTTPS redirection and before Angular SSR.
  // Health checks should never trigger a render, an API call, or a redirect.
  server.get('/healthz', (_req: Request, res: Response) => {
    res.status(200).type('text/plain').send('ok\n');
  });

  // Endpoints internes appeles par l'API sur le reseau prive. Ils sont proteges
  // par le jeton partage et restent en HTTP Docker, avant la redirection HTTPS publique.
  server.get('/internal/technical-stats', (req: Request, res: Response) => {
    if (!authorizeInternalCacheRequest(req, res)) {
      return;
    }

    res.setHeader('Cache-Control', 'no-store');
    res.status(200).type('application/json').send(JSON.stringify(buildTechnicalStatsSnapshot()));
  });

  server.put('/internal/technical-stats/settings', express.json({ limit: '8kb', type: ['application/json', 'application/*+json'] }), (req: Request, res: Response) => {
    if (!authorizeInternalCacheRequest(req, res)) {
      return;
    }

    const updateResult = updateTechnicalStatsSettings(req.body);
    if (!updateResult.ok) {
      res.status(400).type('application/json').send(JSON.stringify({ error: updateResult.error }));
      return;
    }

    res.setHeader('Cache-Control', 'no-store');
    res.status(200).type('application/json').send(JSON.stringify(updateResult.settings));
  });

  server.post('/internal/cache/invalidate', express.json({ limit: '64kb', type: ['application/json', 'application/*+json'] }), (req: Request, res: Response) => {
    if (!authorizeInternalCacheRequest(req, res)) {
      return;
    }

    const invalidationRequest = normalizeCacheInvalidationRequest(req.body);
    if (invalidationRequest.all) {
      clearPendingTargetedSsrCacheInvalidation();
    }

    const result = invalidationRequest.all
      ? clearAllSsrCaches()
      : enqueueTargetedSsrCacheInvalidation(invalidationRequest);

    recordCacheInvalidation(invalidationRequest, result);

    res.status(200).type('application/json').send(JSON.stringify(result));
  });

  server.use(redirectHttpToHttps);
  server.use(applySecurityHeaders);

  server.head('/robots.txt', (req: Request, res: Response, next: NextFunction) => {
    proxySeoDocumentToApi(req, res, next, req.originalUrl);
  });

  server.get('/robots.txt', (req: Request, res: Response, next: NextFunction) => {
    proxySeoDocumentToApi(req, res, next, req.originalUrl);
  });

  server.head('/sitemap.xml', (req: Request, res: Response, next: NextFunction) => {
    proxySeoDocumentToApi(req, res, next, req.originalUrl);
  });

  server.get('/sitemap.xml', (req: Request, res: Response, next: NextFunction) => {
    proxySeoDocumentToApi(req, res, next, req.originalUrl);
  });

  server.head('/:fileName([A-Za-z0-9_-]+\\.xml)', proxyRootSitemapSectionToApi);

  server.get('/:fileName([A-Za-z0-9_-]+\\.xml)', proxyRootSitemapSectionToApi);

  server.head('/sitemaps/:fileName([A-Za-z0-9_-]+\\.xml)', redirectLegacySitemapSectionRoute);

  server.get('/sitemaps/:fileName([A-Za-z0-9_-]+\\.xml)', redirectLegacySitemapSectionRoute);

  server.head('/:fileName([A-Za-z0-9_-]+\\.txt)', (req: Request, res: Response, next: NextFunction) => {
    proxySeoDocumentToApi(req, res, next, req.originalUrl);
  });

  server.get('/:fileName([A-Za-z0-9_-]+\\.txt)', (req: Request, res: Response, next: NextFunction) => {
    proxySeoDocumentToApi(req, res, next, req.originalUrl);
  });

  server.use('/api', (req: Request, res: Response, next: NextFunction) => {
    const apiPath = req.originalUrl.replace(/^\/api(?=\/|$)/i, '') || '/';
    proxyToApi(req, res, next, apiPath);
  });

  server.get('/version.json', (_req: Request, res: Response) => {
    res.setHeader('Cache-Control', revalidatedStaticAssetCacheControl);
    res.type('application/json').send(JSON.stringify({ version: currentBuildVersion }));
  });

  const legacyVideoShareRoutes: string[] = [
    '/:lang/park/:id/:slug/video/s/:videoId/:videoSlug',
    '/:lang/park/:id/:slug/video/:videoId/:videoSlug',
    '/:lang/park/:id/:slug/item/:itemId/:itemSlug/video/s/:videoId/:videoSlug',
    '/:lang/park/:id/:slug/item/:itemId/:itemSlug/video/:videoId/:videoSlug'
  ];

  server.head(legacyVideoShareRoutes, redirectLegacyVideoShareRoute);
  server.get(legacyVideoShareRoutes, redirectLegacyVideoShareRoute);

  server.use(express.static(browserDistFolder, {
    index: false,
    setHeaders: setStaticAssetResponseHeaders
  }));

  server.get('*', (req: Request, res: Response, next: NextFunction) => {
    if (isAssetLikeRequest(req.originalUrl)) {
      logSampledAssetMiss(req.originalUrl);
      res.status(404).type('text/plain').send('Not found');
      return;
    }

    if (!acceptsHtml(req)) {
      res.status(406).type('text/plain').send('Not acceptable');
      return;
    }

    applySearchRobotsHeaders(req, res);

    const cacheKey = buildPageCacheKey(req);
    const warmupRequest = isSsrWarmupRequest(req);
    const forceWarmupRefresh = warmupRequest && isSsrWarmupRefreshRequest(req);
    const cachedEntry = cacheKey === null || forceWarmupRefresh ? null : getCachedPage(cacheKey);

    if (cachedEntry !== null) {
      res.status(cachedEntry.statusCode);
      const staleEntry = isStalePageCacheEntry(cachedEntry);
      const cacheStatus: SsrPageResponseStatus = staleEntry ? (warmupRequest ? 'WARMUP-STALE' : 'STALE') : (warmupRequest ? 'WARMUP-HIT' : 'HIT');
      setPageCacheResponseHeaders(res, cacheStatus);
      recordPageResponse(req, cacheStatus, cacheKey);
      if (staleEntry && cacheKey !== null && !warmupRequest) {
        enqueueTargetedRefreshes([cacheKey]);
      }
      res.type('html').send(prepareHtmlForResponse(req, res, cachedEntry.html));
      return;
    }

    if (!ssrRenderEnabled || !shouldRenderCacheMiss(req, warmupRequest)) {
      serveCsrFallbackPage(req, res, csrIndexHtml, warmupRequest ? 'CSR-WARMUP-SKIPPED' : 'CSR-CACHE-MISS-FALLBACK');
      return;
    }

    const statusCode: number = resolveSsrRouteStatusCode(req.originalUrl);
    if (statusCode !== 200) {
      res.status(statusCode);
    }

    const publicUrl = getPublicRequestUrl(req);
    scheduleSsrRender(() => commonEngine.render({
      bootstrap: AppServerModule,
      documentFilePath: indexHtml,
      url: publicUrl,
      publicPath: browserDistFolder,
      providers: [
        { provide: APP_BASE_HREF, useValue: req.baseUrl },
        { provide: SSR_RESPONSE, useValue: res }
      ],
    }), req.originalUrl)
      .then((html: string) => {
        if (cacheKey !== null && res.statusCode >= 200 && res.statusCode < 300) {
          setCachedPage(cacheKey, res.statusCode, html);
          const cacheStatus: SsrPageResponseStatus = warmupRequest ? 'WARMED' : 'MISS';
          setPageCacheResponseHeaders(res, cacheStatus);
          recordPageResponse(req, cacheStatus, cacheKey);
        } else {
          recordPageResponse(req, 'SSR-UNCACHED', cacheKey);
        }

        res.send(prepareHtmlForResponse(req, res, html));
      })
      .catch((err: unknown) => {
        if (err instanceof SsrRenderQueueFullError) {
          console.warn(`SSR overload fallback to CSR: active=${activeRenderCount}, queued=${pendingRenderQueue.length}, url=${req.originalUrl}`);
          serveCsrFallbackPage(req, res, csrIndexHtml, 'CSR-OVERLOAD-FALLBACK');
          return;
        }

        next(err);
      });
  });

  return server;
}

function redirectLegacyVideoShareRoute(req: Request, res: Response, next: NextFunction): void {
  const canonicalPath: string | null = buildCanonicalVideoRouteRedirectPath(req.originalUrl);

  if (canonicalPath === null) {
    next();
    return;
  }

  res.redirect(301, canonicalPath);
}

function authorizeInternalCacheRequest(req: Request, res: Response): boolean {
  if (!cacheInvalidationToken) {
    res.status(404).type('text/plain').send('Not found');
    return false;
  }

  const providedToken = req.headers['x-amusementpark-cache-token'];
  const token = Array.isArray(providedToken) ? providedToken[0] : providedToken;

  if (token !== cacheInvalidationToken) {
    res.status(403).type('text/plain').send('Forbidden');
    return false;
  }

  return true;
}

function buildTechnicalStatsSnapshot(): Record<string, unknown> {
  const generatedAtUtc = new Date();
  const uptimeSeconds = Math.max(0, Math.round((generatedAtUtc.getTime() - technicalStatsStartedAtUtc.getTime()) / 1000));
  const diskStats = measureDiskPageCache();
  const totalRenders = technicalStatsCounters.totalRenders;

  return {
    isAvailable: true,
    unavailableReason: null,
    generatedAtUtc: generatedAtUtc.toISOString(),
    startedAtUtc: technicalStatsStartedAtUtc.toISOString(),
    uptimeSeconds,
    buildVersion: currentBuildVersion,
    cache: {
      pageResponses: technicalStatsCounters.pageResponses,
      cacheablePageResponses: technicalStatsCounters.cacheablePageResponses,
      cacheHitResponses: technicalStatsCounters.cacheHitResponses,
      hitRatePercent: toPercent(technicalStatsCounters.cacheHitResponses, technicalStatsCounters.pageResponses),
      robotPageResponses: technicalStatsCounters.robotPageResponses,
      robotCacheHitResponses: technicalStatsCounters.robotCacheHitResponses,
      robotHitRatePercent: toPercent(technicalStatsCounters.robotCacheHitResponses, technicalStatsCounters.robotPageResponses),
      statuses: buildCountRows(pageResponseStatusCounts, technicalStatsCounters.pageResponses),
      robotFamilies: buildRobotFamilyRows()
    },
    storage: {
      memoryEntries: pageCache.size,
      memoryMaxEntries: pageCacheMaxEntries,
      diskEnabled: diskPageCacheEnabled,
      diskEntries: diskStats.entries,
      diskBytes: diskStats.bytes,
      diskMaxBytes: diskPageCacheMaxBytes,
      diskWrites: diskPageCacheWriteCount,
      technicalStatsPersistenceEntries,
      technicalStatsPersistenceBytes,
      technicalStatsPersistencePurgedBuckets,
      seoDocumentEntries: seoDocumentCache.size,
      seoDocumentMaxEntries: seoDocumentCacheMaxEntries,
      seoDocumentRequests: technicalStatsCounters.seoDocumentRequests,
      seoDocumentHits: technicalStatsCounters.seoDocumentHits,
      seoDocumentMisses: technicalStatsCounters.seoDocumentMisses,
      assetMisses: assetMissCount
    },
    rendering: {
      ssrRenderEnabled,
      renderOnCacheMiss,
      renderCriticalRoutesOnCacheMiss,
      activeRenders: activeRenderCount,
      queuedRenders: pendingRenderQueue.length,
      maxConcurrency: renderMaxConcurrency,
      maxQueueEntries: renderQueueMaxEntries,
      totalRenders,
      averageRenderMilliseconds: totalRenders > 0 ? Math.round(technicalStatsCounters.totalRenderMilliseconds / totalRenders) : 0,
      maxRenderMilliseconds: technicalStatsCounters.maxRenderMilliseconds,
      slowRenders: technicalStatsCounters.slowRenders,
      slowRenderThresholdMilliseconds,
      queueFullRejections: technicalStatsCounters.renderQueueFullRejections
    },
    refresh: {
      enabled: targetedRefreshEnabled,
      pendingRefreshes: pendingTargetedRefreshQueue.length,
      activeRefreshes: activeTargetedRefreshCount,
      deduplicatedRefreshKeys: queuedTargetedRefreshKeys.size,
      queuedRefreshes: technicalStatsCounters.targetedRefreshQueued,
      succeededRefreshes: technicalStatsCounters.targetedRefreshSucceeded,
      failedRefreshes: technicalStatsCounters.targetedRefreshFailed,
      maxUrls: targetedRefreshMaxUrls,
      concurrency: targetedRefreshConcurrency,
      delayMilliseconds: targetedRefreshDelayMilliseconds,
      timeoutSeconds: targetedRefreshTimeoutSeconds
    },
    invalidation: {
      requests: technicalStatsCounters.invalidationRequests,
      allRequests: technicalStatsCounters.invalidationAllRequests,
      targetedRequests: technicalStatsCounters.invalidationTargetedRequests,
      clearedEntries: technicalStatsCounters.invalidationClearedEntries,
      staleEntries: technicalStatsCounters.invalidationStaleEntries,
      queuedRefreshes: technicalStatsCounters.invalidationQueuedRefreshes,
      lastInvalidationUtc: technicalStatsCounters.lastInvalidationUtc
    },
    config: {
      pageCacheTtlSeconds,
      stalePageCacheSeconds,
      pageCacheMaxHtmlBytes,
      pageCacheBrowserCacheControl,
      csrFallbackCacheControl,
      seoDocumentBrowserCacheControl,
      technicalStatsPersistenceEnabled,
      technicalStatsPersistenceRetentionDays: technicalStatsRetentionDays,
      technicalStatsPersistenceFlushIntervalSeconds,
      technicalStatsPersistenceLastFlushUtc,
      technicalStatsPersistenceLastCleanupUtc
    }
  };
}

function buildCountRows<TKey>(map: Map<TKey, number>, total: number): TechnicalStatsCount[] {
  return Array.from(map.entries())
    .map(([key, count]: [TKey, number]): TechnicalStatsCount => ({
      key: String(key),
      count,
      percent: toPercent(count, total)
    }))
    .sort((left: TechnicalStatsCount, right: TechnicalStatsCount): number => right.count - left.count || left.key.localeCompare(right.key));
}

function buildRobotFamilyRows(): TechnicalStatsRobotFamily[] {
  return Array.from(robotFamilyCounts.entries())
    .map(([key, count]: [string, number]): TechnicalStatsRobotFamily => {
      const cacheHits = robotFamilyCacheHitCounts.get(key) ?? 0;
      return {
        key,
        count,
        cacheHits,
        hitRatePercent: toPercent(cacheHits, count)
      };
    })
    .sort((left: TechnicalStatsRobotFamily, right: TechnicalStatsRobotFamily): number => right.count - left.count || left.key.localeCompare(right.key));
}

function toPercent(value: number, total: number): number {
  if (total <= 0) {
    return 0;
  }

  return Math.round((value / total) * 1000) / 10;
}

function normalizeInteger(value: number | string | null | undefined, fallback: number, min: number, max: number): number {
  const parsed = typeof value === 'number' ? value : Number(value);

  if (!Number.isFinite(parsed)) {
    return fallback;
  }

  return Math.min(max, Math.max(min, Math.round(parsed)));
}

function measureDiskPageCache(): DiskPageCacheStats {
  if (!diskPageCacheEnabled || !existsSync(diskPageCacheDirectory)) {
    return { entries: 0, bytes: 0 };
  }

  let entries = 0;
  let bytes = 0;

  try {
    for (const fileName of readdirSync(diskPageCacheDirectory)) {
      if (!fileName.endsWith('.json')) {
        continue;
      }

      try {
        const stats = statSync(join(diskPageCacheDirectory, fileName));
        if (!stats.isFile()) {
          continue;
        }

        entries += 1;
        bytes += stats.size;
      } catch {
        // Best effort technical snapshot only.
      }
    }
  } catch (error: unknown) {
    console.warn('SSR disk page cache stats failed', error);
  }

  return { entries, bytes };
}

function initializeTechnicalStatsPersistence(): void {
  if (!technicalStatsPersistenceEnabled) {
    return;
  }

  try {
    ensureTechnicalStatsPersistenceDirectory();
    loadTechnicalStatsPersistenceSettings();
    technicalStatsPersistencePurgedBuckets = purgeExpiredTechnicalStatsBuckets();
    loadPersistedTechnicalStatsBuckets();
    updateTechnicalStatsPersistenceMeasurements();
    rememberPersistedTechnicalStatsBaseline();
  } catch (error: unknown) {
    console.warn('SSR technical stats persistence initialization failed', error);
  }
}

function startTechnicalStatsPersistenceTimers(): void {
  if (!technicalStatsPersistenceEnabled || technicalStatsPersistenceTimerStarted) {
    return;
  }

  technicalStatsPersistenceTimerStarted = true;

  const flushTimer = setInterval((): void => {
    persistTechnicalStatsBucket();
  }, technicalStatsPersistenceFlushIntervalSeconds * 1000);
  flushTimer.unref();

  const cleanupTimer = setInterval((): void => {
    cleanupTechnicalStatsPersistence();
  }, 60 * 60 * 1000);
  cleanupTimer.unref();
}

function persistTechnicalStatsBucket(): void {
  if (!technicalStatsPersistenceEnabled) {
    return;
  }

  try {
    ensureTechnicalStatsPersistenceDirectory();

    const today = toUtcDateKey(new Date());
    const existingBucket = readTechnicalStatsBucket(today);
    const nowIso = new Date().toISOString();
    const deltaCounters = diffTechnicalStatsCounters(technicalStatsCounters, lastPersistedTechnicalStatsCounters);
    const deltaStatusCounts = diffNumberMaps(pageResponseStatusCounts, lastPersistedPageResponseStatusCounts);
    const deltaRobotCounts = diffNumberMaps(robotFamilyCounts, lastPersistedRobotFamilyCounts);
    const deltaRobotCacheHitCounts = diffNumberMaps(robotFamilyCacheHitCounts, lastPersistedRobotFamilyCacheHitCounts);
    const hasDelta = hasTechnicalStatsCountersDelta(deltaCounters)
      || deltaStatusCounts.size > 0
      || deltaRobotCounts.size > 0
      || deltaRobotCacheHitCounts.size > 0;

    if (!hasDelta) {
      return;
    }

    const bucket: TechnicalStatsPersistentBucket = {
      version: 1,
      date: today,
      startedAtUtc: existingBucket?.startedAtUtc ?? technicalStatsStartedAtUtc.toISOString(),
      updatedAtUtc: nowIso,
      counters: mergeTechnicalStatsCounters(existingBucket?.counters ?? createEmptyTechnicalStatsCounters(), deltaCounters),
      pageResponseStatusCounts: mergeRecordCounts(existingBucket?.pageResponseStatusCounts ?? {}, mapToRecord(deltaStatusCounts)),
      robotFamilyCounts: mergeRecordCounts(existingBucket?.robotFamilyCounts ?? {}, mapToRecord(deltaRobotCounts)),
      robotFamilyCacheHitCounts: mergeRecordCounts(existingBucket?.robotFamilyCacheHitCounts ?? {}, mapToRecord(deltaRobotCacheHitCounts))
    };

    writeTechnicalStatsBucket(bucket);
    technicalStatsPersistenceLastFlushUtc = nowIso;
    rememberPersistedTechnicalStatsBaseline();
    updateTechnicalStatsPersistenceMeasurements();
  } catch (error: unknown) {
    console.warn('SSR technical stats persistence flush failed', error);
  }
}

function cleanupTechnicalStatsPersistence(): void {
  if (!technicalStatsPersistenceEnabled) {
    return;
  }

  persistTechnicalStatsBucket();
  technicalStatsPersistencePurgedBuckets = purgeExpiredTechnicalStatsBuckets();
  technicalStatsPersistenceLastCleanupUtc = new Date().toISOString();
  loadPersistedTechnicalStatsBuckets();
  updateTechnicalStatsPersistenceMeasurements();
  rememberPersistedTechnicalStatsBaseline();
}

function loadTechnicalStatsPersistenceSettings(): void {
  if (!existsSync(technicalStatsSettingsFilePath)) {
    return;
  }

  try {
    const raw = readFileSync(technicalStatsSettingsFilePath, 'utf8');
    const parsed = JSON.parse(raw) as Partial<TechnicalStatsPersistenceSettings>;
    technicalStatsRetentionDays = normalizeInteger(
      parsed.retentionDays,
      technicalStatsDefaultRetentionDays,
      technicalStatsMinRetentionDays,
      technicalStatsMaxRetentionDays);
  } catch (error: unknown) {
    console.warn('SSR technical stats settings load failed', error);
  }
}

function updateTechnicalStatsSettings(body: unknown): { readonly ok: true; readonly settings: Record<string, number> } | { readonly ok: false; readonly error: string } {
  if (!isObject(body)) {
    return { ok: false, error: 'invalid-body' };
  }

  const requestedRetentionDays = Number(body['persistenceRetentionDays']);
  if (!Number.isFinite(requestedRetentionDays)) {
    return { ok: false, error: 'invalid-retention-days' };
  }

  technicalStatsRetentionDays = normalizeInteger(
    requestedRetentionDays,
    technicalStatsDefaultRetentionDays,
    technicalStatsMinRetentionDays,
    technicalStatsMaxRetentionDays);

  persistTechnicalStatsSettings();
  cleanupTechnicalStatsPersistence();

  return {
    ok: true,
    settings: buildTechnicalStatsSettingsSnapshot()
  };
}

function persistTechnicalStatsSettings(): void {
  if (!technicalStatsPersistenceEnabled) {
    return;
  }

  try {
    ensureTechnicalStatsPersistenceDirectory();
    const settings: TechnicalStatsPersistenceSettings = {
      retentionDays: technicalStatsRetentionDays,
      updatedAtUtc: new Date().toISOString()
    };
    writeJsonAtomically(technicalStatsSettingsFilePath, settings);
  } catch (error: unknown) {
    console.warn('SSR technical stats settings save failed', error);
  }
}

function buildTechnicalStatsSettingsSnapshot(): Record<string, number> {
  return {
    persistenceRetentionDays: technicalStatsRetentionDays
  };
}

function loadPersistedTechnicalStatsBuckets(): void {
  const buckets = readRetainedTechnicalStatsBuckets();

  resetTechnicalStatsCounters();
  pageResponseStatusCounts.clear();
  robotFamilyCounts.clear();
  robotFamilyCacheHitCounts.clear();

  if (buckets.length === 0) {
    technicalStatsStartedAtUtc = new Date();
    return;
  }

  let earliestStartedAtUtc: string | null = null;

  for (const bucket of buckets) {
    mergeCountersIntoTechnicalStats(bucket.counters);
    mergeRecordIntoMap(bucket.pageResponseStatusCounts, pageResponseStatusCounts);
    mergeRecordIntoMap(bucket.robotFamilyCounts, robotFamilyCounts);
    mergeRecordIntoMap(bucket.robotFamilyCacheHitCounts, robotFamilyCacheHitCounts);

    if (earliestStartedAtUtc === null || bucket.startedAtUtc < earliestStartedAtUtc) {
      earliestStartedAtUtc = bucket.startedAtUtc;
    }
  }

  technicalStatsStartedAtUtc = earliestStartedAtUtc ? new Date(earliestStartedAtUtc) : new Date();
}

function readRetainedTechnicalStatsBuckets(): TechnicalStatsPersistentBucket[] {
  if (!existsSync(technicalStatsPersistenceDirectory)) {
    return [];
  }

  const cutoff = getTechnicalStatsRetentionCutoffDateKey();
  const buckets: TechnicalStatsPersistentBucket[] = [];

  for (const fileName of readdirSync(technicalStatsPersistenceDirectory)) {
    const bucketDate = parseTechnicalStatsBucketFileDate(fileName);
    if (bucketDate === null || bucketDate < cutoff) {
      continue;
    }

    const bucket = readTechnicalStatsBucket(bucketDate);
    if (bucket !== null) {
      buckets.push(bucket);
    }
  }

  return buckets.sort((left: TechnicalStatsPersistentBucket, right: TechnicalStatsPersistentBucket): number => left.date.localeCompare(right.date));
}

function readTechnicalStatsBucket(dateKey: string): TechnicalStatsPersistentBucket | null {
  const filePath = getTechnicalStatsBucketPath(dateKey);

  if (!existsSync(filePath)) {
    return null;
  }

  try {
    const parsed = JSON.parse(readFileSync(filePath, 'utf8')) as Partial<TechnicalStatsPersistentBucket>;

    if (parsed.version !== 1 || parsed.date !== dateKey || typeof parsed.startedAtUtc !== 'string' || typeof parsed.updatedAtUtc !== 'string') {
      return null;
    }

    return {
      version: 1,
      date: dateKey,
      startedAtUtc: parsed.startedAtUtc,
      updatedAtUtc: parsed.updatedAtUtc,
      counters: normalizeTechnicalStatsCounters(parsed.counters),
      pageResponseStatusCounts: normalizeCountRecord(parsed.pageResponseStatusCounts),
      robotFamilyCounts: normalizeCountRecord(parsed.robotFamilyCounts),
      robotFamilyCacheHitCounts: normalizeCountRecord(parsed.robotFamilyCacheHitCounts)
    };
  } catch (error: unknown) {
    console.warn(`SSR technical stats bucket read failed: ${dateKey}`, error);
    return null;
  }
}

function writeTechnicalStatsBucket(bucket: TechnicalStatsPersistentBucket): void {
  writeJsonAtomically(getTechnicalStatsBucketPath(bucket.date), bucket);
}

function purgeExpiredTechnicalStatsBuckets(): number {
  if (!existsSync(technicalStatsPersistenceDirectory)) {
    return 0;
  }

  const cutoff = getTechnicalStatsRetentionCutoffDateKey();
  let purged = 0;

  for (const fileName of readdirSync(technicalStatsPersistenceDirectory)) {
    const bucketDate = parseTechnicalStatsBucketFileDate(fileName);
    if (bucketDate === null || bucketDate >= cutoff) {
      continue;
    }

    try {
      unlinkSync(join(technicalStatsPersistenceDirectory, fileName));
      purged += 1;
    } catch {
      // Best effort cleanup only.
    }
  }

  return purged;
}

function updateTechnicalStatsPersistenceMeasurements(): void {
  if (!technicalStatsPersistenceEnabled || !existsSync(technicalStatsPersistenceDirectory)) {
    technicalStatsPersistenceEntries = 0;
    technicalStatsPersistenceBytes = 0;
    return;
  }

  let entries = 0;
  let bytes = 0;

  for (const fileName of readdirSync(technicalStatsPersistenceDirectory)) {
    if (parseTechnicalStatsBucketFileDate(fileName) === null) {
      continue;
    }

    try {
      const fileStats = statSync(join(technicalStatsPersistenceDirectory, fileName));
      if (!fileStats.isFile()) {
        continue;
      }

      entries += 1;
      bytes += fileStats.size;
    } catch {
      // Best effort measurement only.
    }
  }

  technicalStatsPersistenceEntries = entries;
  technicalStatsPersistenceBytes = bytes;
}

function ensureTechnicalStatsPersistenceDirectory(): void {
  if (!existsSync(technicalStatsPersistenceDirectory)) {
    mkdirSync(technicalStatsPersistenceDirectory, { recursive: true });
  }
}

function getTechnicalStatsBucketPath(dateKey: string): string {
  return join(technicalStatsPersistenceDirectory, `bucket-${dateKey}.json`);
}

function parseTechnicalStatsBucketFileDate(fileName: string): string | null {
  const match = /^bucket-(\d{4}-\d{2}-\d{2})\.json$/.exec(fileName);
  return match ? match[1] : null;
}

function getTechnicalStatsRetentionCutoffDateKey(): string {
  const now = new Date();
  const todayUtc = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
  const cutoff = new Date(todayUtc - (technicalStatsRetentionDays - 1) * 86400000);
  return toUtcDateKey(cutoff);
}

function toUtcDateKey(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function writeJsonAtomically(filePath: string, value: unknown): void {
  const temporaryPath = `${filePath}.${process.pid}.tmp`;
  writeFileSync(temporaryPath, JSON.stringify(value), 'utf8');
  renameSync(temporaryPath, filePath);
}

function rememberPersistedTechnicalStatsBaseline(): void {
  lastPersistedTechnicalStatsCounters = cloneTechnicalStatsCounters(technicalStatsCounters);
  lastPersistedPageResponseStatusCounts = cloneNumberMap(pageResponseStatusCounts);
  lastPersistedRobotFamilyCounts = cloneNumberMap(robotFamilyCounts);
  lastPersistedRobotFamilyCacheHitCounts = cloneNumberMap(robotFamilyCacheHitCounts);
}

function createEmptyTechnicalStatsCounters(): TechnicalStatsCounters {
  return {
    pageResponses: 0,
    cacheablePageResponses: 0,
    cacheHitResponses: 0,
    robotPageResponses: 0,
    robotCacheHitResponses: 0,
    totalRenders: 0,
    totalRenderMilliseconds: 0,
    maxRenderMilliseconds: 0,
    slowRenders: 0,
    renderQueueFullRejections: 0,
    targetedRefreshQueued: 0,
    targetedRefreshSucceeded: 0,
    targetedRefreshFailed: 0,
    invalidationRequests: 0,
    invalidationAllRequests: 0,
    invalidationTargetedRequests: 0,
    invalidationClearedEntries: 0,
    invalidationStaleEntries: 0,
    invalidationQueuedRefreshes: 0,
    lastInvalidationUtc: null,
    seoDocumentRequests: 0,
    seoDocumentHits: 0,
    seoDocumentMisses: 0
  };
}

function cloneTechnicalStatsCounters(source: TechnicalStatsCounters): TechnicalStatsCounters {
  return { ...source };
}

function normalizeTechnicalStatsCounters(source: unknown): TechnicalStatsCounters {
  if (!isObject(source)) {
    return createEmptyTechnicalStatsCounters();
  }

  const counters = createEmptyTechnicalStatsCounters();
  for (const key of Object.keys(counters) as Array<keyof TechnicalStatsCounters>) {
    if (key === 'lastInvalidationUtc') {
      counters.lastInvalidationUtc = typeof source[key] === 'string' ? source[key] : null;
      continue;
    }

    counters[key] = Math.max(0, Number(source[key] ?? 0)) as never;
  }

  return counters;
}

function resetTechnicalStatsCounters(): void {
  Object.assign(technicalStatsCounters, createEmptyTechnicalStatsCounters());
}

function diffTechnicalStatsCounters(current: TechnicalStatsCounters, baseline: TechnicalStatsCounters): TechnicalStatsCounters {
  return {
    pageResponses: Math.max(0, current.pageResponses - baseline.pageResponses),
    cacheablePageResponses: Math.max(0, current.cacheablePageResponses - baseline.cacheablePageResponses),
    cacheHitResponses: Math.max(0, current.cacheHitResponses - baseline.cacheHitResponses),
    robotPageResponses: Math.max(0, current.robotPageResponses - baseline.robotPageResponses),
    robotCacheHitResponses: Math.max(0, current.robotCacheHitResponses - baseline.robotCacheHitResponses),
    totalRenders: Math.max(0, current.totalRenders - baseline.totalRenders),
    totalRenderMilliseconds: Math.max(0, current.totalRenderMilliseconds - baseline.totalRenderMilliseconds),
    maxRenderMilliseconds: current.maxRenderMilliseconds > baseline.maxRenderMilliseconds ? current.maxRenderMilliseconds : 0,
    slowRenders: Math.max(0, current.slowRenders - baseline.slowRenders),
    renderQueueFullRejections: Math.max(0, current.renderQueueFullRejections - baseline.renderQueueFullRejections),
    targetedRefreshQueued: Math.max(0, current.targetedRefreshQueued - baseline.targetedRefreshQueued),
    targetedRefreshSucceeded: Math.max(0, current.targetedRefreshSucceeded - baseline.targetedRefreshSucceeded),
    targetedRefreshFailed: Math.max(0, current.targetedRefreshFailed - baseline.targetedRefreshFailed),
    invalidationRequests: Math.max(0, current.invalidationRequests - baseline.invalidationRequests),
    invalidationAllRequests: Math.max(0, current.invalidationAllRequests - baseline.invalidationAllRequests),
    invalidationTargetedRequests: Math.max(0, current.invalidationTargetedRequests - baseline.invalidationTargetedRequests),
    invalidationClearedEntries: Math.max(0, current.invalidationClearedEntries - baseline.invalidationClearedEntries),
    invalidationStaleEntries: Math.max(0, current.invalidationStaleEntries - baseline.invalidationStaleEntries),
    invalidationQueuedRefreshes: Math.max(0, current.invalidationQueuedRefreshes - baseline.invalidationQueuedRefreshes),
    lastInvalidationUtc: current.lastInvalidationUtc !== baseline.lastInvalidationUtc ? current.lastInvalidationUtc : null,
    seoDocumentRequests: Math.max(0, current.seoDocumentRequests - baseline.seoDocumentRequests),
    seoDocumentHits: Math.max(0, current.seoDocumentHits - baseline.seoDocumentHits),
    seoDocumentMisses: Math.max(0, current.seoDocumentMisses - baseline.seoDocumentMisses)
  };
}

function hasTechnicalStatsCountersDelta(counters: TechnicalStatsCounters): boolean {
  return counters.pageResponses > 0
    || counters.cacheablePageResponses > 0
    || counters.cacheHitResponses > 0
    || counters.robotPageResponses > 0
    || counters.robotCacheHitResponses > 0
    || counters.totalRenders > 0
    || counters.totalRenderMilliseconds > 0
    || counters.maxRenderMilliseconds > 0
    || counters.slowRenders > 0
    || counters.renderQueueFullRejections > 0
    || counters.targetedRefreshQueued > 0
    || counters.targetedRefreshSucceeded > 0
    || counters.targetedRefreshFailed > 0
    || counters.invalidationRequests > 0
    || counters.invalidationAllRequests > 0
    || counters.invalidationTargetedRequests > 0
    || counters.invalidationClearedEntries > 0
    || counters.invalidationStaleEntries > 0
    || counters.invalidationQueuedRefreshes > 0
    || counters.lastInvalidationUtc !== null
    || counters.seoDocumentRequests > 0
    || counters.seoDocumentHits > 0
    || counters.seoDocumentMisses > 0;
}

function mergeTechnicalStatsCounters(left: TechnicalStatsCounters, right: TechnicalStatsCounters): TechnicalStatsCounters {
  return {
    pageResponses: left.pageResponses + right.pageResponses,
    cacheablePageResponses: left.cacheablePageResponses + right.cacheablePageResponses,
    cacheHitResponses: left.cacheHitResponses + right.cacheHitResponses,
    robotPageResponses: left.robotPageResponses + right.robotPageResponses,
    robotCacheHitResponses: left.robotCacheHitResponses + right.robotCacheHitResponses,
    totalRenders: left.totalRenders + right.totalRenders,
    totalRenderMilliseconds: left.totalRenderMilliseconds + right.totalRenderMilliseconds,
    maxRenderMilliseconds: Math.max(left.maxRenderMilliseconds, right.maxRenderMilliseconds),
    slowRenders: left.slowRenders + right.slowRenders,
    renderQueueFullRejections: left.renderQueueFullRejections + right.renderQueueFullRejections,
    targetedRefreshQueued: left.targetedRefreshQueued + right.targetedRefreshQueued,
    targetedRefreshSucceeded: left.targetedRefreshSucceeded + right.targetedRefreshSucceeded,
    targetedRefreshFailed: left.targetedRefreshFailed + right.targetedRefreshFailed,
    invalidationRequests: left.invalidationRequests + right.invalidationRequests,
    invalidationAllRequests: left.invalidationAllRequests + right.invalidationAllRequests,
    invalidationTargetedRequests: left.invalidationTargetedRequests + right.invalidationTargetedRequests,
    invalidationClearedEntries: left.invalidationClearedEntries + right.invalidationClearedEntries,
    invalidationStaleEntries: left.invalidationStaleEntries + right.invalidationStaleEntries,
    invalidationQueuedRefreshes: left.invalidationQueuedRefreshes + right.invalidationQueuedRefreshes,
    lastInvalidationUtc: getLatestIsoDate(left.lastInvalidationUtc, right.lastInvalidationUtc),
    seoDocumentRequests: left.seoDocumentRequests + right.seoDocumentRequests,
    seoDocumentHits: left.seoDocumentHits + right.seoDocumentHits,
    seoDocumentMisses: left.seoDocumentMisses + right.seoDocumentMisses
  };
}

function mergeCountersIntoTechnicalStats(counters: TechnicalStatsCounters): void {
  const merged = mergeTechnicalStatsCounters(technicalStatsCounters, counters);
  Object.assign(technicalStatsCounters, merged);
}

function getLatestIsoDate(left: string | null, right: string | null): string | null {
  if (left === null) {
    return right;
  }

  if (right === null) {
    return left;
  }

  return left >= right ? left : right;
}

function cloneNumberMap<TKey>(source: Map<TKey, number>): Map<TKey, number> {
  return new Map<TKey, number>(source.entries());
}

function diffNumberMaps<TKey>(current: Map<TKey, number>, baseline: Map<TKey, number>): Map<TKey, number> {
  const delta = new Map<TKey, number>();

  for (const [key, value] of current.entries()) {
    const count = Math.max(0, value - (baseline.get(key) ?? 0));
    if (count > 0) {
      delta.set(key, count);
    }
  }

  return delta;
}

function mapToRecord<TKey>(source: Map<TKey, number>): Record<string, number> {
  const record: Record<string, number> = {};
  for (const [key, value] of source.entries()) {
    record[String(key)] = value;
  }

  return record;
}

function normalizeCountRecord(source: unknown): Record<string, number> {
  if (!isObject(source)) {
    return {};
  }

  const record: Record<string, number> = {};
  for (const [key, value] of Object.entries(source)) {
    const count = Number(value);
    if (Number.isFinite(count) && count > 0) {
      record[key] = Math.round(count);
    }
  }

  return record;
}

function mergeRecordCounts(left: Record<string, number>, right: Record<string, number>): Record<string, number> {
  const merged: Record<string, number> = { ...left };

  for (const [key, value] of Object.entries(right)) {
    merged[key] = (merged[key] ?? 0) + value;
  }

  return merged;
}

function mergeRecordIntoMap<TKey extends string>(source: Record<string, number>, target: Map<TKey, number>): void {
  for (const [key, value] of Object.entries(source)) {
    target.set(key as TKey, (target.get(key as TKey) ?? 0) + value);
  }
}

function run(): void {
  const port = Number(process.env['PORT'] ?? 4000);
  const server = app();

  startTechnicalStatsPersistenceTimers();

  server.listen(port, () => {
    console.log(`Angular SSR server listening on http://0.0.0.0:${port}`);
    console.log(`Application build version: ${currentBuildVersion}`);
    console.log(`SSR API internal origin: ${apiInternalOrigin}`);
    console.log(`SSR public page cache: ${pageCacheTtlSeconds}s / ${pageCacheMaxEntries} entries`);
    console.log(`SSR disk page cache: ${diskPageCacheEnabled ? 'enabled' : 'disabled'} / ${diskPageCacheDirectory} / ${diskPageCacheMaxBytes} bytes`);
    console.log(`SSR disk page cache budget check: every ${diskPageCacheBudgetCheckEveryWrites} writes`);
    console.log(`SSR page cache max HTML bytes: ${pageCacheMaxHtmlBytes}`);
    console.log(`SSR public page cache ignores analytics cookies: ${pageCacheAllowAuthenticatedPublicHtml}`);
    console.log(`SSR technical stats persistence: ${technicalStatsPersistenceEnabled ? 'enabled' : 'disabled'} / ${technicalStatsPersistenceDirectory} / retention=${technicalStatsRetentionDays}d / flush=${technicalStatsPersistenceFlushIntervalSeconds}s`);
    console.log(`SSR page browser Cache-Control: ${pageCacheBrowserCacheControl}`);
    console.log(`SSR SEO document cache: ${seoDocumentCacheTtlSeconds}s / ${seoDocumentCacheMaxEntries} entries`);
    console.log(`SSR SEO document browser Cache-Control: ${seoDocumentBrowserCacheControl}`);
    console.log(`SSR stale page cache: ${stalePageCacheSeconds}s / targeted refresh=${targetedRefreshEnabled} / max=${targetedRefreshMaxUrls} / concurrency=${targetedRefreshConcurrency}`);
    console.log(`SSR render enabled: ${ssrRenderEnabled}`);
    console.log(`SSR render on cache miss: ${renderOnCacheMiss}`);
    console.log(`SSR render critical routes on cache miss: ${renderCriticalRoutesOnCacheMiss}`);
    console.log(`SSR render concurrency: ${renderMaxConcurrency} active / ${renderQueueMaxEntries} queued`);
    console.log(`SSR slow render threshold: ${slowRenderThresholdMilliseconds}ms`);
  });
}

function scheduleSsrRender(render: () => Promise<string>, requestUrl?: string): Promise<string> {
  if (activeRenderCount < renderMaxConcurrency) {
    return runScheduledSsrRender(render, requestUrl);
  }

  if (pendingRenderQueue.length >= renderQueueMaxEntries) {
    technicalStatsCounters.renderQueueFullRejections += 1;
    console.warn(`SSR render queue full: active=${activeRenderCount}, queued=${pendingRenderQueue.length}, url=${requestUrl ?? 'unknown'}`);
    return Promise.reject(new SsrRenderQueueFullError());
  }

  const queueLengthAfterPush = pendingRenderQueue.length + 1;
  if (queueLengthAfterPush >= renderQueueWarningThreshold) {
    console.warn(`SSR render queue high: active=${activeRenderCount}, queued=${queueLengthAfterPush}, url=${requestUrl ?? 'unknown'}`);
  }

  return new Promise<string>((resolve: (value: string) => void, reject: (reason?: unknown) => void): void => {
    pendingRenderQueue.push((): void => {
      runScheduledSsrRender(render, requestUrl).then(resolve).catch(reject);
    });
  });
}

function runScheduledSsrRender(render: () => Promise<string>, requestUrl?: string): Promise<string> {
  activeRenderCount += 1;
  const startedAt = Date.now();

  return render().finally((): void => {
    const elapsedMilliseconds = Date.now() - startedAt;
    technicalStatsCounters.totalRenders += 1;
    technicalStatsCounters.totalRenderMilliseconds += elapsedMilliseconds;
    technicalStatsCounters.maxRenderMilliseconds = Math.max(technicalStatsCounters.maxRenderMilliseconds, elapsedMilliseconds);
    if (slowRenderThresholdMilliseconds > 0 && elapsedMilliseconds >= slowRenderThresholdMilliseconds) {
      technicalStatsCounters.slowRenders += 1;
      console.warn(`SSR slow render: ${elapsedMilliseconds}ms, active=${activeRenderCount}, queued=${pendingRenderQueue.length}, url=${requestUrl ?? 'unknown'}`);
    }

    activeRenderCount = Math.max(0, activeRenderCount - 1);
    runNextQueuedSsrRender();
  });
}

function runNextQueuedSsrRender(): void {
  if (activeRenderCount >= renderMaxConcurrency) {
    return;
  }

  const nextRender: (() => void) | undefined = pendingRenderQueue.shift();

  if (nextRender === undefined) {
    return;
  }

  nextRender();
}

function applySearchRobotsHeaders(req: Request, res: Response): void {
  if (shouldApplyNoindexFollowHeader(req.originalUrl)) {
    res.setHeader('X-Robots-Tag', 'noindex, follow');
  }
}

function shouldRenderCacheMiss(req: Request, warmupRequest: boolean): boolean {
  if (!isCacheablePageRequest(req)) {
    return false;
  }

  if (warmupRequest) {
    return true;
  }

  if (renderOnCacheMiss) {
    return true;
  }

  return renderCriticalRoutesOnCacheMiss && isCriticalPublicSsrRoute(req.originalUrl);
}

function serveCsrFallbackPage(req: Request, res: Response, csrIndexHtmlPath: string | null, mode: string): void {
  const statusCode: number = resolveSsrRouteStatusCode(req.originalUrl);
  if (statusCode !== 200) {
    res.status(statusCode);
  }

  csrFallbackCount += 1;
  recordPageResponse(req, toCsrFallbackStatus(mode), buildPageCacheKey(req));
  if (csrFallbackLogSampleRate > 0 && csrFallbackCount % csrFallbackLogSampleRate === 0) {
    console.warn(`SSR CSR fallback sample: count=${csrFallbackCount}, mode=${mode}, url=${req.originalUrl}`);
  }

  res.setHeader('X-AmusementPark-SSR-Mode', mode);
  res.setHeader('X-AmusementPark-Build-Version', currentBuildVersion);
  res.setHeader('Cache-Control', csrFallbackCacheControl);
  res.type('html').send(prepareHtmlForResponse(req, res, readCsrShellHtml(csrIndexHtmlPath)));
}

function prepareHtmlForResponse(req: Request, res: Response, html: string): string {
  if (!robotNoJsHtmlEnabled || detectRobotFamily(req) === null) {
    return html;
  }

  const optimizationResult: RobotHtmlOptimizationResult = optimizeHtmlForRobotNoJs(html);
  if (optimizationResult.removedScriptCount === 0 && optimizationResult.removedScriptLikeLinkCount === 0) {
    return optimizationResult.html;
  }

  res.setHeader('X-AmusementPark-Robot-Html', 'no-js');
  res.setHeader('X-AmusementPark-Robot-Html-Scripts-Removed', optimizationResult.removedScriptCount.toString());
  res.setHeader('X-AmusementPark-Robot-Html-Links-Removed', optimizationResult.removedScriptLikeLinkCount.toString());

  return optimizationResult.html;
}

function toCsrFallbackStatus(mode: string): SsrPageResponseStatus {
  switch (mode) {
    case 'CSR-OVERLOAD-FALLBACK':
      return 'CSR-OVERLOAD-FALLBACK';
    case 'CSR-WARMUP-SKIPPED':
      return 'CSR-WARMUP-SKIPPED';
    default:
      return 'CSR-CACHE-MISS-FALLBACK';
  }
}

function recordPageResponse(req: Request, status: SsrPageResponseStatus, cacheKey: string | null): void {
  technicalStatsCounters.pageResponses += 1;
  incrementCount(pageResponseStatusCounts, status);

  if (cacheKey !== null) {
    technicalStatsCounters.cacheablePageResponses += 1;
  }

  const isCacheHit = isCacheHitStatus(status);
  if (isCacheHit) {
    technicalStatsCounters.cacheHitResponses += 1;
  }

  const robotFamily = detectRobotFamily(req);
  if (robotFamily === null) {
    return;
  }

  technicalStatsCounters.robotPageResponses += 1;
  incrementCount(robotFamilyCounts, robotFamily);

  if (isCacheHit) {
    technicalStatsCounters.robotCacheHitResponses += 1;
    incrementCount(robotFamilyCacheHitCounts, robotFamily);
  }
}

function isCacheHitStatus(status: SsrPageResponseStatus): boolean {
  return status === 'HIT'
    || status === 'STALE'
    || status === 'WARMUP-HIT'
    || status === 'WARMUP-STALE';
}

function detectRobotFamily(req: Request): string | null {
  const userAgent = getHeaderValue(req, 'user-agent').toLowerCase();
  if (userAgent.length === 0 || userAgent.includes('amusementpark-ssr-targetedrefresh')) {
    return null;
  }

  if (userAgent.includes('googlebot') || userAgent.includes('adsbot-google') || userAgent.includes('mediapartners-google')) {
    return 'Googlebot';
  }

  if (userAgent.includes('bingbot') || userAgent.includes('msnbot')) {
    return 'Bingbot';
  }

  if (userAgent.includes('duckduckbot')) {
    return 'DuckDuckBot';
  }

  if (userAgent.includes('yandexbot')) {
    return 'YandexBot';
  }

  if (userAgent.includes('ahrefsbot')) {
    return 'AhrefsBot';
  }

  if (userAgent.includes('semrushbot')) {
    return 'SemrushBot';
  }

  if (/(?:bot|crawler|spider|slurp|facebookexternalhit|whatsapp|telegrambot|linkedinbot|pinterest|discordbot|twitterbot)/i.test(userAgent)) {
    return 'Other bot';
  }

  return null;
}

function getHeaderValue(req: Request, headerName: string): string {
  const headerValue = req.headers[headerName.toLowerCase()];
  if (Array.isArray(headerValue)) {
    return headerValue.join(' ');
  }

  return headerValue ?? '';
}

function incrementCount<TValue>(map: Map<TValue, number>, key: TValue, count = 1): void {
  map.set(key, (map.get(key) ?? 0) + count);
}

function setStaticAssetResponseHeaders(res: Response, filePath: string): void {
  res.setHeader('X-AmusementPark-Build-Version', currentBuildVersion);
  res.setHeader('Cache-Control', isImmutableStaticAsset(filePath) ? immutableBuildAssetCacheControl : revalidatedStaticAssetCacheControl);
}

function isImmutableStaticAsset(filePath: string): boolean {
  return isImmutableBuildAsset(filePath) || isLocalFontAsset(filePath);
}

function isImmutableBuildAsset(filePath: string): boolean {
  const normalizedPath: string = filePath.replace(/\\/g, '/');
  const fileName: string = normalizedPath.split('/').pop() ?? '';

  return /\.(?:js|mjs|css)$/i.test(fileName) && /-[a-z0-9]{8,}\.(?:js|mjs|css)$/i.test(fileName);
}

function isLocalFontAsset(filePath: string): boolean {
  const normalizedPath: string = filePath.replace(/\\/g, '/');

  return /\/assets\/fonts\/.+\.(?:ttf|woff2?)$/i.test(normalizedPath);
}

function readCsrShellHtml(csrIndexHtmlPath: string | null): string {
  if (cachedCsrShellHtml !== null) {
    return cachedCsrShellHtml;
  }

  if (csrIndexHtmlPath !== null && existsSync(csrIndexHtmlPath)) {
    cachedCsrShellHtml = readFileSync(csrIndexHtmlPath, 'utf8');
    return cachedCsrShellHtml;
  }

  cachedCsrShellHtml = '<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Amusement Park</title><meta name="viewport" content="width=device-width, initial-scale=1"></head><body><app-root></app-root><script>location.reload()</script></body></html>';
  console.error('CSR shell index.html was not found. Served emergency minimal shell.');
  return cachedCsrShellHtml;
}

function resolveBrowserDistFolder(serverDistFolder: string): string {
  return resolveFirstExistingDirectory([
    process.env['SSR_BROWSER_DIST_FOLDER'] ?? '',
    resolve(serverDistFolder, '../browser'),
    resolve(serverDistFolder, '..'),
    resolve(process.cwd(), 'dist/amusement-park/browser'),
    resolve(process.cwd(), 'dist/amusement-park')
  ], 'browser dist folder');
}

function resolveIndexServerHtml(serverDistFolder: string, browserDistFolder: string): string {
  return resolveFirstExistingFile([
    process.env['SSR_INDEX_SERVER_HTML'] ?? '',
    join(serverDistFolder, 'index.server.html'),
    join(browserDistFolder, 'index.server.html'),
    join(browserDistFolder, 'index.html')
  ], 'SSR index document') ?? join(serverDistFolder, 'index.server.html');
}

function resolveCsrIndexHtml(serverDistFolder: string, browserDistFolder: string): string | null {
  return resolveFirstExistingFile([
    process.env['SSR_CSR_INDEX_HTML'] ?? '',
    join(browserDistFolder, 'index.html'),
    join(browserDistFolder, 'index.csr.html'),
    join(browserDistFolder, 'index.original.html'),
    join(serverDistFolder, '../browser/index.html'),
    join(serverDistFolder, '../index.html')
  ], 'CSR index document');
}

function resolveFirstExistingDirectory(candidates: string[], label: string): string {
  const existingPath: string | undefined = candidates.find((candidate: string): boolean => candidate.length > 0 && existsSync(candidate) && statSync(candidate).isDirectory());
  if (existingPath) {
    console.log(`Resolved ${label}: ${existingPath}`);
    return existingPath;
  }

  const fallbackPath: string = candidates.find((candidate: string): boolean => candidate.length > 0) ?? process.cwd();
  console.error(`Unable to resolve ${label}. Falling back to ${fallbackPath}. Candidates: ${candidates.filter((candidate: string): boolean => candidate.length > 0).join('; ')}`);
  return fallbackPath;
}

function resolveFirstExistingFile(candidates: string[], label: string): string | null {
  const existingPath: string | undefined = candidates.find((candidate: string): boolean => candidate.length > 0 && existsSync(candidate) && statSync(candidate).isFile());
  if (existingPath) {
    console.log(`Resolved ${label}: ${existingPath}`);
    return existingPath;
  }

  console.error(`Unable to resolve ${label}. Candidates: ${candidates.filter((candidate: string): boolean => candidate.length > 0).join('; ')}`);
  return null;
}

function buildPageCacheKey(req: Request): string | null {
  if (pageCacheTtlSeconds <= 0 || pageCacheMaxEntries <= 0) {
    return null;
  }

  if (!isCacheablePageRequest(req)) {
    return null;
  }

  const host = getForwardedValue(req, 'x-forwarded-host') ?? req.headers.host ?? 'localhost';
  const protocol = getForwardedValue(req, 'x-forwarded-proto') ?? req.protocol;
  return `${protocol}://${host}${req.originalUrl}`;
}

function isCacheablePageRequest(req: Request): boolean {
  if (req.method !== 'GET' && req.method !== 'HEAD') {
    return false;
  }

  if (req.headers.authorization) {
    return false;
  }

  if (!pageCacheAllowAuthenticatedPublicHtml && containsAuthenticationCookie(req)) {
    return false;
  }

  if (!isPublicSsrCacheRoute(req.originalUrl)) {
    return false;
  }

  return acceptsHtml(req);
}

function acceptsHtml(req: Request): boolean {
  const acceptHeader = req.headers.accept ?? '';
  if (Array.isArray(acceptHeader)) {
    return acceptHeader.some((value: string) => value.includes('text/html') || value.includes('*/*'));
  }

  return acceptHeader.length === 0 || acceptHeader.includes('text/html') || acceptHeader.includes('*/*');
}

function isPublicSsrCacheRoute(url: string): boolean {
  const path = getPathOnly(url);

  return isPublicStaticSsrRoute(path)
    || isPublicParkDetailRoute(path)
    || isPublicParkImagesRoute(path)
    || isPublicParkVideosRoute(path)
    || isPublicParkVideoDetailRoute(path)
    || isPublicParkWeatherRoute(path)
    || isPublicParkZonesRoute(path)
    || isPublicParkZoneDetailRoute(path)
    || isPublicParkItemsRoute(path)
    || isPublicParkItemDetailRoute(path)
    || isPublicParkItemImagesRoute(path)
    || isPublicParkItemVideosRoute(path)
    || isPublicParkItemVideoDetailRoute(path)
    || isPublicReferenceRoute(path);
}

function isCriticalPublicSsrRoute(url: string): boolean {
  const path = getPathOnly(url);

  return isPublicStaticSsrRoute(path)
    || isPublicParkDetailRoute(path)
    || (isPublicParkImagesRoute(path) && !hasQueryString(url))
    || (isPublicParkVideosRoute(path) && !hasQueryString(url))
    || isPublicParkVideoDetailRoute(path)
    || (isPublicParkWeatherRoute(path) && !hasQueryString(url))
    || isPublicParkZonesRoute(path)
    || isPublicParkZoneDetailRoute(path)
    || (isPublicParkItemsRoute(path) && !hasQueryString(url))
    || isPublicParkItemDetailRoute(path)
    || (isPublicParkItemImagesRoute(path) && !hasQueryString(url))
    || (isPublicParkItemVideosRoute(path) && !hasQueryString(url))
    || isPublicParkItemVideoDetailRoute(path)
    || isPublicReferenceRoute(path);
}

function isPublicStaticSsrRoute(path: string): boolean {
  return /^\/?$/i.test(path)
    || /^\/[a-z]{2}\/?$/i.test(path)
    || /^\/[a-z]{2}\/home\/?$/i.test(path)
    || /^\/[a-z]{2}\/parks\/?$/i.test(path)
    || /^\/[a-z]{2}\/rankings\/?$/i.test(path)
    || /^\/[a-z]{2}\/manufacturers\/?$/i.test(path)
    || /^\/[a-z]{2}\/technical(?:\/[^/]+)?\/?$/i.test(path)
    || /^\/[a-z]{2}\/about\/?$/i.test(path)
    || /^\/[a-z]{2}\/contact\/?$/i.test(path)
    || /^\/[a-z]{2}\/versions\/?$/i.test(path)
    || /^\/[a-z]{2}\/privacy\/?$/i.test(path);
}

function isPublicParkDetailRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/?$/i.test(path);
}

function isPublicParkImagesRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/images\/?$/i.test(path);
}

function isPublicParkVideosRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/videos\/?$/i.test(path);
}

function isPublicParkVideoDetailRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/(?:videos\/[^/]+\/[^/]+|video\/(?:s\/)?[^/]+\/[^/]+)\/?$/i.test(path);
}

function isPublicParkWeatherRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/weather\/?$/i.test(path);
}

function isPublicParkZonesRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/zones\/?$/i.test(path);
}

function isPublicParkZoneDetailRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/zone\/[^/]+\/[^/]+\/?$/i.test(path);
}

function isPublicParkItemsRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/items\/?$/i.test(path);
}

function isPublicParkItemDetailRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/?$/i.test(path);
}

function isPublicParkItemImagesRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/images\/?$/i.test(path);
}

function isPublicParkItemVideosRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/videos\/?$/i.test(path);
}

function isPublicParkItemVideoDetailRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/(?:videos\/[^/]+\/[^/]+|video\/(?:s\/)?[^/]+\/[^/]+)\/?$/i.test(path);
}

function isPublicReferenceRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park-(?:operator|founder|manufacturer)\/[^/]+\/[^/]+\/?$/i.test(path);
}

function hasQueryString(url: string): boolean {
  return url.includes('?');
}

function isAssetLikeRequest(url: string): boolean {
  const path = getPathOnly(url);
  return /\.[a-z0-9][a-z0-9_-]*(?:\.[a-z0-9][a-z0-9_-]*)?$/i.test(path);
}

function getPathOnly(url: string): string {
  const queryIndex = url.indexOf('?');
  const hashIndex = url.indexOf('#');
  if (queryIndex < 0 && hashIndex < 0) {
    return url;
  }

  if (queryIndex < 0) {
    return url.slice(0, hashIndex);
  }

  if (hashIndex < 0) {
    return url.slice(0, queryIndex);
  }

  return url.slice(0, Math.min(queryIndex, hashIndex));
}

function logSampledAssetMiss(url: string): void {
  assetMissCount += 1;
  if (assetMissLogSampleRate > 0 && assetMissCount % assetMissLogSampleRate === 0) {
    console.warn(`Static asset miss bypassed SSR: count=${assetMissCount}, url=${url}`);
  }
}

function getCachedPage(cacheKey: string): PageCacheEntry | null {
  const memoryEntry = pageCache.get(cacheKey);

  if (memoryEntry) {
    if (isUsablePageCacheEntry(memoryEntry)) {
      return memoryEntry;
    }

    pageCache.delete(cacheKey);
  }

  return getDiskCachedPage(cacheKey);
}

function setCachedPage(cacheKey: string, statusCode: number, html: string): void {
  const htmlByteLength: number = Buffer.byteLength(html, 'utf8');
  if (pageCacheMaxHtmlBytes > 0 && htmlByteLength > pageCacheMaxHtmlBytes) {
    console.warn(`SSR page cache skipped: htmlSize=${htmlByteLength}, max=${pageCacheMaxHtmlBytes}, key=${cacheKey}`);
    return;
  }

  const entry: PageCacheEntry = {
    buildVersion: currentBuildVersion,
    cacheKey,
    statusCode,
    html,
    expiresAt: Date.now() + pageCacheTtlSeconds * 1000
  };

  pageCache.set(cacheKey, entry);
  setDiskCachedPage(cacheKey, entry);

  while (pageCache.size > pageCacheMaxEntries) {
    const oldestKey = pageCache.keys().next().value as string | undefined;
    if (!oldestKey) {
      break;
    }

    pageCache.delete(oldestKey);
  }
}

function isUsablePageCacheEntry(entry: PageCacheEntry): boolean {
  const now = Date.now();
  return entry.buildVersion === currentBuildVersion && (entry.expiresAt > now || (entry.staleUntil ?? 0) > now);
}

function isStalePageCacheEntry(entry: PageCacheEntry): boolean {
  return entry.expiresAt <= Date.now() && (entry.staleUntil ?? 0) > Date.now();
}

function setPageCacheResponseHeaders(res: Response, cacheStatus: SsrPageResponseStatus): void {
  res.setHeader('X-AmusementPark-SSR-Cache', cacheStatus);
  res.setHeader('X-AmusementPark-Build-Version', currentBuildVersion);
  res.setHeader('Cache-Control', pageCacheBrowserCacheControl);
}

function getDiskCachedPage(cacheKey: string): PageCacheEntry | null {
  if (!diskPageCacheEnabled || pageCacheTtlSeconds <= 0 || diskPageCacheMaxBytes <= 0) {
    return null;
  }

  const cacheFilePath = getDiskPageCacheFilePath(cacheKey);

  try {
    if (!existsSync(cacheFilePath)) {
      return null;
    }

    const serializedEntry: string = readFileSync(cacheFilePath, 'utf8');
    const parsedEntry = JSON.parse(serializedEntry) as PageCacheEntry;

    if (!isUsablePageCacheEntry(parsedEntry) || typeof parsedEntry.html !== 'string') {
      unlinkSync(cacheFilePath);
      return null;
    }

    pageCache.set(cacheKey, parsedEntry);
    return parsedEntry;
  } catch (error: unknown) {
    console.warn(`SSR disk cache read failed for ${cacheFilePath}`, error);
    return null;
  }
}

function setDiskCachedPage(cacheKey: string, entry: PageCacheEntry): void {
  if (!diskPageCacheEnabled || diskPageCacheMaxBytes <= 0) {
    return;
  }

  try {
    mkdirSync(diskPageCacheDirectory, { recursive: true });
    writeFileSync(getDiskPageCacheFilePath(cacheKey), JSON.stringify(entry), 'utf8');
    diskPageCacheWriteCount += 1;
    if (diskPageCacheWriteCount % diskPageCacheBudgetCheckEveryWrites === 0) {
      enforceDiskPageCacheBudget();
    }
  } catch (error: unknown) {
    console.warn('SSR disk cache write failed', error);
  }
}

function getDiskPageCacheFilePath(cacheKey: string): string {
  const hash: string = createHash('sha256').update(cacheKey).digest('hex');
  return join(diskPageCacheDirectory, `${hash}.json`);
}

function enforceDiskPageCacheBudget(): void {
  if (!diskPageCacheEnabled || diskPageCacheMaxBytes <= 0 || !existsSync(diskPageCacheDirectory)) {
    return;
  }

  const files = readdirSync(diskPageCacheDirectory)
    .filter((fileName: string) => fileName.endsWith('.json'))
    .map((fileName: string) => {
      const filePath: string = join(diskPageCacheDirectory, fileName);
      const stats = statSync(filePath);
      return { filePath, size: stats.size, mtimeMs: stats.mtimeMs };
    })
    .sort((left, right) => left.mtimeMs - right.mtimeMs);

  let totalBytes: number = files.reduce((sum: number, file) => sum + file.size, 0);

  for (const file of files) {
    if (totalBytes <= diskPageCacheMaxBytes) {
      break;
    }

    try {
      unlinkSync(file.filePath);
      totalBytes -= file.size;
    } catch {
      // Best effort cleanup only.
    }
  }
}

function clearAllSsrCaches(): CacheInvalidationResult {
  const pageMemoryEntries: number = pageCache.size;
  const seoDocumentEntries: number = seoDocumentCache.size;

  pageCache.clear();
  seoDocumentCache.clear();
  const pageDiskEntries = clearDiskPageCache();

  return {
    cleared: pageMemoryEntries + seoDocumentEntries + pageDiskEntries,
    pageMemoryEntries,
    pageDiskEntries,
    seoDocumentEntries,
    stalePageEntries: 0,
    queuedRefreshes: 0,
    all: true
  };
}

function clearTargetedSsrCaches(request: NormalizedCacheInvalidationRequest): CacheInvalidationResult {
  const matchedCacheKeys = new Set<string>();
  const pageMemoryEntries = clearMatchingMemoryPageCache(request, matchedCacheKeys);
  const pageDiskEntries = clearMatchingDiskPageCache(request, matchedCacheKeys);
  const seoDocumentEntries = request.includeSeoDocuments ? seoDocumentCache.size : 0;

  if (request.includeSeoDocuments) {
    seoDocumentCache.clear();
  }

  const stalePageEntries = request.allowStale && stalePageCacheSeconds > 0 ? pageMemoryEntries + pageDiskEntries : 0;
  const queuedRefreshes = request.allowStale && request.refresh
    ? enqueueTargetedRefreshes(Array.from(matchedCacheKeys))
    : 0;

  return {
    cleared: pageMemoryEntries + pageDiskEntries + seoDocumentEntries,
    pageMemoryEntries,
    pageDiskEntries,
    seoDocumentEntries,
    stalePageEntries,
    queuedRefreshes,
    all: false
  };
}

function enqueueTargetedSsrCacheInvalidation(request: NormalizedCacheInvalidationRequest): CacheInvalidationResult {
  pendingTargetedInvalidationRequest = pendingTargetedInvalidationRequest === null
    ? request
    : mergeTargetedInvalidationRequests(pendingTargetedInvalidationRequest, request);

  scheduleTargetedSsrCacheInvalidation();

  return {
    cleared: 0,
    pageMemoryEntries: 0,
    pageDiskEntries: 0,
    seoDocumentEntries: 0,
    stalePageEntries: 0,
    queuedRefreshes: 0,
    all: false,
    deferred: true
  };
}

function scheduleTargetedSsrCacheInvalidation(): void {
  if (pendingTargetedInvalidationTimer !== null || targetedInvalidationInProgress) {
    return;
  }

  pendingTargetedInvalidationTimer = setTimeout(processPendingTargetedSsrCacheInvalidation, targetedInvalidationDebounceMilliseconds);
}

function processPendingTargetedSsrCacheInvalidation(): void {
  pendingTargetedInvalidationTimer = null;

  if (targetedInvalidationInProgress || pendingTargetedInvalidationRequest === null) {
    return;
  }

  const request = pendingTargetedInvalidationRequest;
  pendingTargetedInvalidationRequest = null;
  targetedInvalidationInProgress = true;

  try {
    const result = clearTargetedSsrCaches(request);
    recordCacheInvalidationResult(result);
  } catch (error: unknown) {
    console.warn('SSR targeted cache invalidation failed', error);
  } finally {
    targetedInvalidationInProgress = false;

    if (pendingTargetedInvalidationRequest !== null) {
      scheduleTargetedSsrCacheInvalidation();
    }
  }
}

function clearPendingTargetedSsrCacheInvalidation(): void {
  if (pendingTargetedInvalidationTimer !== null) {
    clearTimeout(pendingTargetedInvalidationTimer);
    pendingTargetedInvalidationTimer = null;
  }

  pendingTargetedInvalidationRequest = null;
}

function mergeTargetedInvalidationRequests(
  current: NormalizedCacheInvalidationRequest,
  next: NormalizedCacheInvalidationRequest
): NormalizedCacheInvalidationRequest {
  const allowStale = current.allowStale && next.allowStale;

  return {
    all: false,
    paths: mergeNormalizedStrings(current.paths, next.paths),
    prefixes: mergeNormalizedStrings(current.prefixes, next.prefixes),
    includeSeoDocuments: current.includeSeoDocuments || next.includeSeoDocuments,
    allowStale,
    refresh: allowStale && (current.refresh || next.refresh)
  };
}

function mergeNormalizedStrings(current: string[], next: string[]): string[] {
  return Array.from(new Set([...current, ...next]));
}

function recordCacheInvalidation(request: NormalizedCacheInvalidationRequest, result: CacheInvalidationResult): void {
  technicalStatsCounters.invalidationRequests += 1;
  recordCacheInvalidationResult(result);

  if (request.all) {
    technicalStatsCounters.invalidationAllRequests += 1;
  } else {
    technicalStatsCounters.invalidationTargetedRequests += 1;
  }
}

function recordCacheInvalidationResult(result: CacheInvalidationResult): void {
  technicalStatsCounters.invalidationClearedEntries += result.cleared;
  technicalStatsCounters.invalidationStaleEntries += result.stalePageEntries;
  technicalStatsCounters.invalidationQueuedRefreshes += result.queuedRefreshes;
  technicalStatsCounters.lastInvalidationUtc = new Date().toISOString();
}

function clearMatchingMemoryPageCache(request: NormalizedCacheInvalidationRequest, matchedCacheKeys: Set<string>): number {
  let affected = 0;

  for (const cacheKey of Array.from(pageCache.keys())) {
    if (!isPageCacheKeyMatched(cacheKey, request)) {
      continue;
    }

    const entry = pageCache.get(cacheKey);
    if (entry && request.allowStale && stalePageCacheSeconds > 0) {
      const staleEntry = markPageCacheEntryStale(cacheKey, entry);
      pageCache.set(cacheKey, staleEntry);
      setDiskCachedPage(cacheKey, staleEntry);
      matchedCacheKeys.add(cacheKey);
    } else {
      pageCache.delete(cacheKey);
    }

    affected += 1;
  }

  return affected;
}

function clearMatchingDiskPageCache(request: NormalizedCacheInvalidationRequest, matchedCacheKeys: Set<string>): number {
  if (!diskPageCacheEnabled || !existsSync(diskPageCacheDirectory)) {
    return 0;
  }

  let affected = 0;

  try {
    for (const fileName of readdirSync(diskPageCacheDirectory)) {
      if (!fileName.endsWith('.json')) {
        continue;
      }

      const filePath = join(diskPageCacheDirectory, fileName);

      try {
        const serializedEntry: string = readFileSync(filePath, 'utf8');
        const parsedEntry = JSON.parse(serializedEntry) as PageCacheEntry;

        if (!parsedEntry.cacheKey || isPageCacheKeyMatched(parsedEntry.cacheKey, request)) {
          if (parsedEntry.cacheKey && request.allowStale && stalePageCacheSeconds > 0 && typeof parsedEntry.html === 'string') {
            const staleEntry = markPageCacheEntryStale(parsedEntry.cacheKey, parsedEntry);
            writeFileSync(filePath, JSON.stringify(staleEntry), 'utf8');
            pageCache.set(parsedEntry.cacheKey, staleEntry);
            matchedCacheKeys.add(parsedEntry.cacheKey);
          } else {
            unlinkSync(filePath);
          }

          affected += 1;
        }
      } catch {
        unlinkSync(filePath);
        affected += 1;
      }
    }
  } catch (error: unknown) {
    console.warn('SSR disk page cache targeted clear failed', error);
  }

  return affected;
}

function markPageCacheEntryStale(cacheKey: string, entry: PageCacheEntry): PageCacheEntry {
  const now = Date.now();
  return {
    ...entry,
    cacheKey,
    expiresAt: Math.min(entry.expiresAt, now),
    staleUntil: Math.max(entry.staleUntil ?? 0, now + stalePageCacheSeconds * 1000)
  };
}

function clearDiskPageCache(): number {
  if (!diskPageCacheEnabled || !existsSync(diskPageCacheDirectory)) {
    return 0;
  }

  let removed: number = 0;

  try {
    for (const fileName of readdirSync(diskPageCacheDirectory)) {
      if (!fileName.endsWith('.json')) {
        continue;
      }

      try {
        unlinkSync(join(diskPageCacheDirectory, fileName));
        removed += 1;
      } catch {
        // Best effort cleanup only.
      }
    }
  } catch (error: unknown) {
    console.warn('SSR disk page cache clear failed', error);
  }

  return removed;
}

function normalizeCacheInvalidationRequest(body: unknown): NormalizedCacheInvalidationRequest {
  const request: CacheInvalidationRequest = isObject(body) ? body as CacheInvalidationRequest : {};
  const paths = normalizeInvalidationPaths(request.paths);
  const prefixes = normalizeInvalidationPaths(request.prefixes);
  const includeSeoDocuments = request.includeSeoDocuments === true;
  const all = request.all === true || (paths.length === 0 && prefixes.length === 0 && !includeSeoDocuments);
  const allowStale = !all && request.allowStale !== false;
  const refresh = allowStale && targetedRefreshEnabled && request.refresh !== false;

  return {
    all,
    paths,
    prefixes,
    includeSeoDocuments: all || includeSeoDocuments,
    allowStale,
    refresh
  };
}

function enqueueTargetedRefreshes(cacheKeys: string[]): number {
  if (!targetedRefreshEnabled || targetedRefreshMaxUrls <= 0) {
    return 0;
  }

  let queued = 0;
  for (const cacheKey of cacheKeys) {
    if (queued >= targetedRefreshMaxUrls) {
      break;
    }

    if (!isCacheKeyRefreshable(cacheKey) || queuedTargetedRefreshKeys.has(cacheKey)) {
      continue;
    }

    queuedTargetedRefreshKeys.add(cacheKey);
    pendingTargetedRefreshQueue.push(cacheKey);
    queued += 1;
  }

  technicalStatsCounters.targetedRefreshQueued += queued;
  drainTargetedRefreshQueue();
  return queued;
}

function drainTargetedRefreshQueue(): void {
  while (activeTargetedRefreshCount < targetedRefreshConcurrency && pendingTargetedRefreshQueue.length > 0) {
    const cacheKey = pendingTargetedRefreshQueue.shift();
    if (!cacheKey) {
      continue;
    }

    activeTargetedRefreshCount += 1;
    void refreshTargetedCacheKey(cacheKey)
      .then(() => {
        technicalStatsCounters.targetedRefreshSucceeded += 1;
      })
      .catch((error: unknown) => {
        technicalStatsCounters.targetedRefreshFailed += 1;
        console.warn(`SSR targeted refresh failed: key=${cacheKey}`, error);
      })
      .finally(() => {
        activeTargetedRefreshCount -= 1;
        queuedTargetedRefreshKeys.delete(cacheKey);
        if (targetedRefreshDelayMilliseconds > 0) {
          setTimeout(drainTargetedRefreshQueue, targetedRefreshDelayMilliseconds);
        } else {
          drainTargetedRefreshQueue();
        }
      });
  }
}

function refreshTargetedCacheKey(cacheKey: string): Promise<void> {
  const originalUrl = new URL(cacheKey);
  const localPort = Number(process.env['PORT'] ?? 4000);
  const refreshUrl = new URL(`${originalUrl.pathname}${originalUrl.search}`, `http://127.0.0.1:${localPort}`);
  const client = refreshUrl.protocol === 'https:' ? https : http;

  return new Promise<void>((resolvePromise, rejectPromise) => {
    const request = client.request(refreshUrl, {
      method: 'GET',
      timeout: targetedRefreshTimeoutSeconds * 1000,
      headers: {
        Accept: 'text/html,*/*',
        Host: originalUrl.host,
        'User-Agent': 'AmusementPark-SSR-TargetedRefresh/1.0',
        'X-Forwarded-Host': originalUrl.host,
        'X-Forwarded-Proto': originalUrl.protocol.replace(':', ''),
        'X-AmusementPark-SSR-Warmup': '1',
        'X-AmusementPark-SSR-Warmup-Refresh': '1'
      }
    }, (response) => {
      response.resume();
      response.on('end', () => {
        const statusCode = response.statusCode ?? 0;
        if (statusCode >= 200 && statusCode < 300) {
          resolvePromise();
          return;
        }

        rejectPromise(new Error(`HTTP ${statusCode}`));
      });
    });

    request.on('timeout', () => {
      request.destroy(new Error(`timeout after ${targetedRefreshTimeoutSeconds}s`));
    });
    request.on('error', rejectPromise);
    request.end();
  });
}

function isCacheKeyRefreshable(cacheKey: string): boolean {
  try {
    const parsed = new URL(cacheKey);
    return parsed.protocol === 'http:' || parsed.protocol === 'https:';
  } catch {
    return false;
  }
}

function normalizeInvalidationPaths(values: string[] | undefined): string[] {
  if (!Array.isArray(values)) {
    return [];
  }

  return Array.from(new Set(values
    .map((value: string): string | null => normalizeInvalidationPath(value))
    .filter((value: string | null): value is string => value !== null)));
}

function normalizeInvalidationPath(value: string): string | null {
  if (typeof value !== 'string' || value.trim().length === 0) {
    return null;
  }

  const trimmed = value.trim();
  let path = trimmed;

  try {
    path = new URL(trimmed, 'https://amusement-parks.fun').pathname;
  } catch {
    path = trimmed.split(/[?#]/, 1)[0] ?? trimmed;
  }

  if (!path.startsWith('/')) {
    path = `/${path}`;
  }

  return normalizePathForComparison(path);
}

function isPageCacheKeyMatched(cacheKey: string, request: NormalizedCacheInvalidationRequest): boolean {
  const path = extractPathFromCacheKey(cacheKey);

  if (path === null) {
    return false;
  }

  if (request.paths.some((candidate: string): boolean => path === candidate)) {
    return true;
  }

  return request.prefixes.some((prefix: string): boolean => path === prefix || path.startsWith(ensureTrailingSlash(prefix)));
}

function extractPathFromCacheKey(cacheKey: string): string | null {
  try {
    return normalizePathForComparison(new URL(cacheKey).pathname);
  } catch {
    const schemeIndex = cacheKey.indexOf('://');
    const pathStart = schemeIndex >= 0 ? cacheKey.indexOf('/', schemeIndex + 3) : cacheKey.indexOf('/');

    if (pathStart < 0) {
      return null;
    }

    return normalizePathForComparison(cacheKey.slice(pathStart).split(/[?#]/, 1)[0] ?? '');
  }
}

function normalizePathForComparison(path: string): string {
  if (path.length > 1 && path.endsWith('/')) {
    return path.slice(0, -1);
  }

  return path;
}

function ensureTrailingSlash(path: string): string {
  return path.endsWith('/') ? path : `${path}/`;
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function containsAuthenticationCookie(req: Request): boolean {
  const cookieHeader = req.headers.cookie;

  if (!cookieHeader) {
    return false;
  }

  const headerValue: string = Array.isArray(cookieHeader) ? cookieHeader.join(';') : cookieHeader;
  return /(?:^|;\s*)(amusementpark\.refresh|amusementpark\.auth|\.AspNetCore\.|access_token|refresh_token)=/i.test(headerValue);
}

function isSsrWarmupRequest(req: Request): boolean {
  const headerValue = req.headers['x-amusementpark-ssr-warmup'];

  if (Array.isArray(headerValue)) {
    return headerValue.some((value: string) => value === '1');
  }

  return headerValue === '1';
}

function isSsrWarmupRefreshRequest(req: Request): boolean {
  const headerValue = req.headers['x-amusementpark-ssr-warmup-refresh'];

  if (Array.isArray(headerValue)) {
    return headerValue.some((value: string) => value === '1' || value.toLowerCase() === 'true');
  }

  return headerValue === '1' || headerValue?.toLowerCase() === 'true';
}

function redirectHttpToHttps(req: Request, res: Response, next: NextFunction): void {
  if (!forceHttps || req.method === 'OPTIONS') {
    next();
    return;
  }

  const forwardedProto = getForwardedValue(req, 'x-forwarded-proto');
  const protocol = forwardedProto ?? req.protocol;

  if (protocol.toLowerCase() === 'https') {
    next();
    return;
  }

  const forwardedHost = getForwardedValue(req, 'x-forwarded-host');
  const host = forwardedHost ?? req.headers.host;

  if (!host) {
    next();
    return;
  }

  res.redirect(308, `https://${host}${req.originalUrl}`);
}

function applySecurityHeaders(_req: Request, res: Response, next: NextFunction): void {
  res.setHeader('X-Content-Type-Options', 'nosniff');
  res.setHeader('X-Frame-Options', 'DENY');
  res.setHeader('Referrer-Policy', 'strict-origin-when-cross-origin');
  res.setHeader('Permissions-Policy', 'camera=(), microphone=(), geolocation=()');

  if (cspEnabled) {
    const cspHeaderName = cspReportOnly ? 'Content-Security-Policy-Report-Only' : 'Content-Security-Policy';
    res.setHeader(cspHeaderName, buildContentSecurityPolicy());
  }

  next();
}

function buildContentSecurityPolicy(): string {
  const reportUri = process.env['SSR_CSP_REPORT_URI'] ?? '/api/security/csp-report';
  const localScriptSources: string[] = allowLocalCspSources ? ['http://localhost:*', 'http://matomo.amusement.localhost:*'] : [];
  const localImageSources: string[] = allowLocalCspSources ? ['http://localhost:*', 'http://amusement.localhost:*', 'http://matomo.amusement.localhost:*'] : [];
  const localConnectSources: string[] = allowLocalCspSources ? ['http://localhost:*', 'https://localhost:*', 'http://amusement.localhost:*', 'http://matomo.amusement.localhost:*'] : [];

  return [
    "default-src 'self'",
    "base-uri 'self'",
    "object-src 'none'",
    "frame-ancestors 'none'",
    "form-action 'self'",
    joinCspDirective('script-src', ["'self'", "'unsafe-inline'", 'https://accounts.google.com', 'https://apis.google.com', 'https://matomo.cedric-caudron.com', 'https://www.clarity.ms', 'https://*.clarity.ms', ...localScriptSources]),
    joinCspDirective('style-src', ["'self'", "'unsafe-inline'", 'https://accounts.google.com']),
    joinCspDirective('style-src-elem', ["'self'", "'unsafe-inline'", 'https://accounts.google.com']),
    joinCspDirective('font-src', ["'self'", 'data:']),
    joinCspDirective('img-src', ["'self'", 'data:', 'blob:', 'https:', 'https://tile.openstreetmap.org', 'https://*.tile.openstreetmap.org', 'https://*.clarity.ms', ...localImageSources]),
    joinCspDirective('connect-src', ["'self'", 'https://accounts.google.com', 'https://www.googleapis.com', 'https://matomo.cedric-caudron.com', 'https://www.clarity.ms', 'https://*.clarity.ms', ...localConnectSources]),
    joinCspDirective('frame-src', ["'self'", 'https://accounts.google.com']),
    "worker-src 'self' blob:",
    "media-src 'self' blob: data:",
    "manifest-src 'self'",
    `report-uri ${reportUri}`
  ].join('; ');
}

function joinCspDirective(name: string, sources: string[]): string {
  const uniqueSources: string[] = Array.from(new Set(sources.filter((source: string) => source.length > 0)));
  return `${name} ${uniqueSources.join(' ')}`;
}

function proxySeoDocumentToApi(req: Request, res: Response, next: NextFunction, targetPath: string): void {
  const normalizedMethod = req.method.toUpperCase();
  if (!isSeoDocumentCacheEnabled() || !isSeoDocumentCacheableMethod(normalizedMethod)) {
    proxyToApi(req, res, next, targetPath);
    return;
  }

  technicalStatsCounters.seoDocumentRequests += 1;
  const cacheKey = buildSeoDocumentCacheKey(targetPath);
  const cachedEntry = getCachedSeoDocument(cacheKey);

  if (cachedEntry !== null) {
    technicalStatsCounters.seoDocumentHits += 1;
    writeCachedSeoDocument(req, res, cachedEntry, 'HIT');
    return;
  }

  if (normalizedMethod === 'HEAD') {
    proxyToApi(req, res, next, targetPath);
    return;
  }

  getOrFetchSeoDocument(req, targetPath, cacheKey, (error: Error | null, entry: SeoDocumentCacheEntry | null) => {
    if (error !== null) {
      next(error);
      return;
    }

    if (entry === null) {
      proxyToApi(req, res, next, targetPath);
      return;
    }

    technicalStatsCounters.seoDocumentMisses += 1;
    writeCachedSeoDocument(req, res, entry, 'MISS');
  });
}

function isSeoDocumentCacheEnabled(): boolean {
  return seoDocumentCacheTtlSeconds >= 0 && seoDocumentCacheMaxEntries > 0;
}

function isSeoDocumentCacheableMethod(method: string): boolean {
  return method === 'GET' || method === 'HEAD';
}

function buildSeoDocumentCacheKey(targetPath: string): string {
  return targetPath.toLowerCase();
}

function getCachedSeoDocument(cacheKey: string): SeoDocumentCacheEntry | null {
  const entry = seoDocumentCache.get(cacheKey);
  if (!entry) {
    return null;
  }

  if (entry.expiresAt !== null && entry.expiresAt <= Date.now()) {
    seoDocumentCache.delete(cacheKey);
    return null;
  }

  return entry;
}

function setCachedSeoDocument(cacheKey: string, entry: SeoDocumentCacheEntry): void {
  seoDocumentCache.set(cacheKey, entry);

  while (seoDocumentCache.size > seoDocumentCacheMaxEntries) {
    const oldestKey = seoDocumentCache.keys().next().value as string | undefined;
    if (!oldestKey) {
      break;
    }

    seoDocumentCache.delete(oldestKey);
  }
}

function getOrFetchSeoDocument(req: Request, targetPath: string, cacheKey: string, callback: SeoDocumentCacheCallback): void {
  const pendingCallbacks = pendingSeoDocumentCacheRequests.get(cacheKey);
  if (pendingCallbacks) {
    pendingCallbacks.push(callback);
    return;
  }

  pendingSeoDocumentCacheRequests.set(cacheKey, [callback]);

  fetchSeoDocumentFromApi(req, targetPath, (error: Error | null, entry: SeoDocumentCacheEntry | null) => {
    if (entry !== null && entry.statusCode >= 200 && entry.statusCode < 300) {
      setCachedSeoDocument(cacheKey, entry);
    }

    const callbacks = pendingSeoDocumentCacheRequests.get(cacheKey) ?? [];
    pendingSeoDocumentCacheRequests.delete(cacheKey);

    callbacks.forEach((pendingCallback: SeoDocumentCacheCallback) => {
      pendingCallback(error, entry);
    });
  });
}

function fetchSeoDocumentFromApi(
  req: Request,
  targetPath: string,
  callback: (error: Error | null, entry: SeoDocumentCacheEntry | null) => void
): void {
  const targetUrl = new URL(targetPath, apiInternalOrigin);
  const client = targetUrl.protocol === 'https:' ? https : http;
  const headers = buildSeoDocumentFetchHeaders(req, targetUrl);

  const proxyRequest = client.request(
    targetUrl,
    {
      method: 'GET',
      headers
    },
    (proxyResponse: http.IncomingMessage) => {
      const chunks: Buffer[] = [];

      proxyResponse.on('data', (chunk: Buffer | string) => {
        chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
      });

      proxyResponse.on('end', () => {
        const body = Buffer.concat(chunks);
        const responseHeaders = buildCachedSeoDocumentHeaders(proxyResponse.headers, body.length);
        const entry: SeoDocumentCacheEntry = {
          statusCode: proxyResponse.statusCode ?? 502,
          headers: responseHeaders,
          body,
          expiresAt: seoDocumentCacheTtlSeconds === 0 ? null : Date.now() + seoDocumentCacheTtlSeconds * 1000
        };

        callback(null, entry);
      });
    }
  );

  proxyRequest.on('error', (error: Error) => callback(error, null));
  proxyRequest.end();
}

function buildCachedSeoDocumentHeaders(headers: http.IncomingHttpHeaders, bodyLength: number): Record<string, string | string[]> {
  const responseHeaders: Record<string, string | string[]> = {};

  Object.entries(headers).forEach(([name, value]: [string, string | string[] | undefined]) => {
    if (value === undefined || isApiHeaderHiddenFromPublicProxy(name) || isHopByHopHeader(name)) {
      return;
    }

    const normalizedName = name.toLowerCase();
    if (normalizedName === 'cache-control' || normalizedName === 'vary') {
      return;
    }

    responseHeaders[name] = value;
  });

  responseHeaders['content-length'] = bodyLength.toString();

  responseHeaders['cache-control'] = seoDocumentBrowserCacheControl;

  return responseHeaders;
}

function writeCachedSeoDocument(req: Request, res: Response, entry: SeoDocumentCacheEntry, cacheStatus: 'HIT' | 'MISS'): void {
  res.status(entry.statusCode);

  Object.entries(entry.headers).forEach(([name, value]: [string, string | string[]]) => {
    res.setHeader(name, value);
  });

  res.setHeader('X-AmusementPark-SEO-Cache', cacheStatus);

  if (req.method.toUpperCase() === 'HEAD') {
    res.end();
    return;
  }

  res.send(entry.body);
}

function proxyRootSitemapSectionToApi(req: Request, res: Response, next: NextFunction): void {
  const fileName = req.params['fileName'];
  if (!fileName || fileName.toLowerCase() === 'sitemap.xml') {
    next();
    return;
  }

  proxySeoDocumentToApi(req, res, next, `/sitemaps/${fileName}`);
}

function redirectLegacySitemapSectionRoute(req: Request, res: Response): void {
  const fileName = req.params['fileName'];
  res.redirect(308, `/${fileName}`);
}

function isHopByHopHeader(name: string): boolean {
  const normalizedName = name.toLowerCase();
  return normalizedName === 'connection'
    || normalizedName === 'keep-alive'
    || normalizedName === 'proxy-authenticate'
    || normalizedName === 'proxy-authorization'
    || normalizedName === 'te'
    || normalizedName === 'trailer'
    || normalizedName === 'transfer-encoding'
    || normalizedName === 'upgrade';
}

function proxyToApi(req: Request, res: Response, next: NextFunction, targetPath: string): void {
  const targetUrl = new URL(targetPath, apiInternalOrigin);
  const client = targetUrl.protocol === 'https:' ? https : http;
  const headers = buildApiProxyHeaders(req, targetUrl);

  const proxyRequest = client.request(
    targetUrl,
    {
      method: req.method,
      headers
    },
    (proxyResponse: http.IncomingMessage) => {
      res.status(proxyResponse.statusCode ?? 502);

      Object.entries(proxyResponse.headers).forEach(([name, value]: [string, string | string[] | undefined]) => {
        if (value === undefined || isApiHeaderHiddenFromPublicProxy(name)) {
          return;
        }

        res.setHeader(name, value);
      });

      proxyResponse.pipe(res);
    }
  );

  proxyRequest.on('error', (error: Error) => next(error));
  req.pipe(proxyRequest);
}

function buildApiProxyHeaders(req: Request, targetUrl: URL): http.OutgoingHttpHeaders {
  const publicProtocol = getForwardedValue(req, 'x-forwarded-proto') ?? req.protocol;
  const publicHost = getForwardedValue(req, 'x-forwarded-host') ?? req.headers.host ?? targetUrl.host;
  const headers: http.OutgoingHttpHeaders = { ...req.headers };
  delete headers['host'];
  delete headers[internalSsrHeaderName];
  delete headers[internalSsrHeaderName.toLowerCase()];
  headers['host'] = publicHost;
  headers['x-forwarded-host'] = publicHost;
  headers['x-forwarded-proto'] = publicProtocol;
  headers['x-forwarded-for'] = appendForwardedFor(req);

  if (req.originalUrl.toLowerCase().startsWith('/api')) {
    headers['x-forwarded-prefix'] = '/api';
  }

  return headers;
}

function buildSeoDocumentFetchHeaders(req: Request, targetUrl: URL): http.OutgoingHttpHeaders {
  const publicProtocol = getForwardedValue(req, 'x-forwarded-proto') ?? req.protocol;
  const publicHost = getForwardedValue(req, 'x-forwarded-host') ?? req.headers.host ?? targetUrl.host;

  const headers: http.OutgoingHttpHeaders = {
    'host': publicHost,
    'x-forwarded-host': publicHost,
    'x-forwarded-proto': publicProtocol,
    'x-forwarded-for': appendForwardedFor(req),
    'accept': 'application/xml, text/plain, */*',
    'accept-language': 'en',
    'user-agent': 'AmusementPark-SSR-SeoCache/1.0',
    [internalSsrHeaderName]: '1',
  };

  return headers;
}

function appendForwardedFor(req: Request): string {
  const existing = getForwardedValue(req, 'x-forwarded-for');
  const remoteAddress = req.socket.remoteAddress ?? '';

  if (!existing) {
    return remoteAddress;
  }

  if (!remoteAddress) {
    return existing;
  }

  return `${existing}, ${remoteAddress}`;
}

function getPublicRequestUrl(req: Request): string {
  const forwardedProto = getForwardedValue(req, 'x-forwarded-proto');
  const forwardedHost = getForwardedValue(req, 'x-forwarded-host');
  const protocol = forwardedProto ?? req.protocol;
  const host = forwardedHost ?? req.headers.host ?? 'localhost';

  return `${protocol}://${host}${req.originalUrl}`;
}

function getForwardedValue(req: Request, headerName: string): string | null {
  const value = req.headers[headerName.toLowerCase()];

  if (Array.isArray(value)) {
    return value[0] ?? null;
  }

  if (!value) {
    return null;
  }

  return value.split(',')[0]?.trim() || null;
}

function normalizeOrigin(value: string): string {
  const normalizedValue = value.trim().replace(/\/+$/, '');

  if (!normalizedValue) {
    return defaultApiInternalOrigin;
  }

  return normalizedValue;
}

function splitConfiguredValues(value: string): string[] {
  const values = value
    .split(/[;,]/)
    .map((item: string) => item.trim())
    .filter((item: string) => item.length > 0);

  if (values.length === 0) {
    return ['localhost', '127.0.0.1', 'amusement.localhost'];
  }

  return Array.from(new Set(values));
}

run();
