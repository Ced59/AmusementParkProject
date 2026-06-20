import { APP_BASE_HREF } from '@angular/common';
import { CommonEngine } from '@angular/ssr/node';
import express, { NextFunction, Request, Response } from 'express';
import http from 'node:http';
import https from 'node:https';
import { dirname, join, resolve } from 'node:path';
import { Buffer } from 'node:buffer';
import { createHash } from 'node:crypto';
import { existsSync, mkdirSync, readdirSync, readFileSync, statSync, unlinkSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import AppServerModule from './src/main.server';
import { SSR_RESPONSE } from './src/app/core/ssr/ssr-response.token';
import { resolveSsrRouteStatusCode, shouldApplyNoindexFollowHeader } from './src/app/core/ssr/ssr-route-status.helpers';
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
let activeRenderCount = 0;
let assetMissCount = 0;
let csrFallbackCount = 0;
let cachedCsrShellHtml: string | null = null;
let diskPageCacheWriteCount = 0;
const pendingRenderQueue: Array<() => void> = [];
const pendingTargetedRefreshQueue: string[] = [];
const queuedTargetedRefreshKeys = new Set<string>();
let activeTargetedRefreshCount = 0;

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

  server.head('/sitemaps/:fileName', (req: Request, res: Response, next: NextFunction) => {
    proxySeoDocumentToApi(req, res, next, req.originalUrl);
  });

  server.get('/sitemaps/:fileName', (req: Request, res: Response, next: NextFunction) => {
    proxySeoDocumentToApi(req, res, next, req.originalUrl);
  });

  server.head('/:fileName([A-Za-z0-9_-]+\\.txt)', (req: Request, res: Response, next: NextFunction) => {
    proxySeoDocumentToApi(req, res, next, req.originalUrl);
  });

  server.get('/:fileName([A-Za-z0-9_-]+\\.txt)', (req: Request, res: Response, next: NextFunction) => {
    proxySeoDocumentToApi(req, res, next, req.originalUrl);
  });

  // Endpoint interne (réseau privé only) déclenché par l'API après une écriture
  // de contenu public : purge le cache de pages SSR pour rendre les
  // modifications immédiatement visibles. Protégé par un jeton partagé ;
  // désactivé (404) si aucun jeton n'est configuré.
  server.post('/internal/cache/invalidate', express.json({ limit: '64kb', type: ['application/json', 'application/*+json'] }), (req: Request, res: Response) => {
    if (!cacheInvalidationToken) {
      res.status(404).type('text/plain').send('Not found');
      return;
    }

    const providedToken = req.headers['x-amusementpark-cache-token'];
    const token = Array.isArray(providedToken) ? providedToken[0] : providedToken;

    if (token !== cacheInvalidationToken) {
      res.status(403).type('text/plain').send('Forbidden');
      return;
    }

    const invalidationRequest = normalizeCacheInvalidationRequest(req.body);
    const result = invalidationRequest.all
      ? clearAllSsrCaches()
      : clearTargetedSsrCaches(invalidationRequest);

    res.status(200).type('application/json').send(JSON.stringify(result));
  });

  server.use('/api', (req: Request, res: Response, next: NextFunction) => {
    const apiPath = req.originalUrl.replace(/^\/api(?=\/|$)/i, '') || '/';
    proxyToApi(req, res, next, apiPath);
  });

  server.get('/version.json', (_req: Request, res: Response) => {
    res.setHeader('Cache-Control', revalidatedStaticAssetCacheControl);
    res.type('application/json').send(JSON.stringify({ version: currentBuildVersion }));
  });

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
      setPageCacheResponseHeaders(res, staleEntry ? (warmupRequest ? 'WARMUP-STALE' : 'STALE') : (warmupRequest ? 'WARMUP-HIT' : 'HIT'));
      if (staleEntry && cacheKey !== null && !warmupRequest) {
        enqueueTargetedRefreshes([cacheKey]);
      }
      res.type('html').send(cachedEntry.html);
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
          setPageCacheResponseHeaders(res, warmupRequest ? 'WARMED' : 'MISS');
        }

        res.send(html);
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

function run(): void {
  const port = Number(process.env['PORT'] ?? 4000);
  const server = app();

  server.listen(port, () => {
    console.log(`Angular SSR server listening on http://0.0.0.0:${port}`);
    console.log(`Application build version: ${currentBuildVersion}`);
    console.log(`SSR API internal origin: ${apiInternalOrigin}`);
    console.log(`SSR public page cache: ${pageCacheTtlSeconds}s / ${pageCacheMaxEntries} entries`);
    console.log(`SSR disk page cache: ${diskPageCacheEnabled ? 'enabled' : 'disabled'} / ${diskPageCacheDirectory} / ${diskPageCacheMaxBytes} bytes`);
    console.log(`SSR disk page cache budget check: every ${diskPageCacheBudgetCheckEveryWrites} writes`);
    console.log(`SSR page cache max HTML bytes: ${pageCacheMaxHtmlBytes}`);
    console.log(`SSR public page cache ignores analytics cookies: ${pageCacheAllowAuthenticatedPublicHtml}`);
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
    if (slowRenderThresholdMilliseconds > 0 && elapsedMilliseconds >= slowRenderThresholdMilliseconds) {
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
  if (csrFallbackLogSampleRate > 0 && csrFallbackCount % csrFallbackLogSampleRate === 0) {
    console.warn(`SSR CSR fallback sample: count=${csrFallbackCount}, mode=${mode}, url=${req.originalUrl}`);
  }

  res.setHeader('X-AmusementPark-SSR-Mode', mode);
  res.setHeader('X-AmusementPark-Build-Version', currentBuildVersion);
  res.setHeader('Cache-Control', csrFallbackCacheControl);
  res.type('html').send(readCsrShellHtml(csrIndexHtmlPath));
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

function setPageCacheResponseHeaders(res: Response, cacheStatus: 'HIT' | 'MISS' | 'WARMED' | 'WARMUP-HIT' | 'STALE' | 'WARMUP-STALE'): void {
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
      .catch((error: unknown) => {
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

  const cacheKey = buildSeoDocumentCacheKey(targetPath);
  const cachedEntry = getCachedSeoDocument(cacheKey);

  if (cachedEntry !== null) {
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

function isApiHeaderHiddenFromPublicProxy(name: string): boolean {
  const normalizedName = name.toLowerCase();
  return normalizedName === 'content-security-policy'
    || normalizedName === 'content-security-policy-report-only'
    || normalizedName === 'x-powered-by';
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
