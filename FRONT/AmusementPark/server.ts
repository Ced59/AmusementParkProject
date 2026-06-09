import { APP_BASE_HREF } from '@angular/common';
import { CommonEngine } from '@angular/ssr/node';
import express, { NextFunction, Request, Response } from 'express';
import http from 'node:http';
import https from 'node:https';
import { dirname, join, resolve } from 'node:path';
import { Buffer } from 'node:buffer';
import { fileURLToPath } from 'node:url';
import AppServerModule from './src/main.server';
import { SSR_RESPONSE } from './src/app/core/ssr/ssr-response.token';

const defaultApiInternalOrigin = 'http://api:8080';
const apiInternalOrigin = normalizeOrigin(process.env['SSR_API_INTERNAL_URL'] ?? defaultApiInternalOrigin);
const cspReportOnly = (process.env['SSR_CSP_REPORT_ONLY'] ?? 'true').toLowerCase() !== 'false';
const cspEnabled = (process.env['SSR_CSP_ENABLED'] ?? 'true').toLowerCase() !== 'false';
const ssrAllowedHosts = splitConfiguredValues(process.env['SSR_ALLOWED_HOSTS'] ?? 'localhost;127.0.0.1;amusement.localhost;front');
const forceHttps = (process.env['SSR_FORCE_HTTPS'] ?? 'false').toLowerCase() === 'true';
const ssrRenderEnabled = (process.env['SSR_RENDER_ENABLED'] ?? 'true').toLowerCase() !== 'false';
const allowLocalCspSources = (process.env['SSR_CSP_ALLOW_LOCAL_DEV_SOURCES'] ?? 'false').toLowerCase() === 'true';
const pageCacheTtlSeconds = Math.max(0, Number(process.env['SSR_PAGE_CACHE_SECONDS'] ?? 30));
const pageCacheMaxEntries = Math.max(0, Number(process.env['SSR_PAGE_CACHE_MAX_ENTRIES'] ?? 250));
const pageCache = new Map<string, PageCacheEntry>();
const seoDocumentCacheTtlSeconds = Math.max(0, Number(process.env['SSR_SEO_DOCUMENT_CACHE_SECONDS'] ?? 3600));
const seoDocumentCacheMaxEntries = Math.max(0, Number(process.env['SSR_SEO_DOCUMENT_CACHE_MAX_ENTRIES'] ?? 128));
const seoDocumentCache = new Map<string, SeoDocumentCacheEntry>();
const pendingSeoDocumentCacheRequests = new Map<string, Array<SeoDocumentCacheCallback>>();
const internalSsrHeaderName = 'X-AmusementPark-Internal-SSR';
const renderMaxConcurrency = Math.max(1, Number(process.env['SSR_RENDER_MAX_CONCURRENCY'] ?? 2));
const renderQueueMaxEntries = Math.max(0, Number(process.env['SSR_RENDER_QUEUE_MAX_ENTRIES'] ?? 20));
const slowRenderThresholdMilliseconds = Math.max(0, Number(process.env['SSR_SLOW_RENDER_THRESHOLD_MILLISECONDS'] ?? 3000));
const renderQueueWarningThreshold = Math.max(1, Number(process.env['SSR_RENDER_QUEUE_WARNING_THRESHOLD'] ?? Math.max(1, Math.floor(renderQueueMaxEntries * 0.75))));
let activeRenderCount = 0;
const pendingRenderQueue: Array<() => void> = [];

interface PageCacheEntry {
  readonly statusCode: number;
  readonly html: string;
  readonly expiresAt: number;
}

interface SeoDocumentCacheEntry {
  readonly statusCode: number;
  readonly headers: Record<string, string | string[]>;
  readonly body: Buffer;
  readonly expiresAt: number;
}

type SeoDocumentCacheCallback = (error: Error | null, entry: SeoDocumentCacheEntry | null) => void;

class SsrRenderQueueFullError extends Error {
  constructor() {
    super('SSR render queue is full.');
    this.name = 'SsrRenderQueueFullError';
  }
}

// The Express app is exported so that it can be used by serverless Functions.
export function app(): express.Express {
  const server = express();
  const serverDistFolder = dirname(fileURLToPath(import.meta.url));
  const browserDistFolder = resolve(serverDistFolder, '../browser');
  const indexHtml = join(serverDistFolder, 'index.server.html');

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

  server.use('/api', (req: Request, res: Response, next: NextFunction) => {
    const apiPath = req.originalUrl.replace(/^\/api(?=\/|$)/i, '') || '/';
    proxyToApi(req, res, next, apiPath);
  });

  server.get('*.*', express.static(browserDistFolder, {
    immutable: true,
    index: false,
    maxAge: '1y'
  }));

  server.get('*', (req: Request, res: Response, next: NextFunction) => {
    if (!ssrRenderEnabled) {
      serveCsrFallbackPage(req, res, browserDistFolder);
      return;
    }

    const publicUrl = getPublicRequestUrl(req);
    const cacheKey = buildPageCacheKey(req);
    const cachedEntry = cacheKey === null ? null : getCachedPage(cacheKey);

    if (cachedEntry !== null) {
      res.status(cachedEntry.statusCode);
      res.setHeader('X-AmusementPark-SSR-Cache', 'HIT');
      res.type('html').send(cachedEntry.html);
      return;
    }

    if (isExplicitNotFoundRoute(req.originalUrl)) {
      res.status(404);
    }

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
          res.setHeader('X-AmusementPark-SSR-Cache', 'MISS');
        }

        res.send(html);
      })
      .catch((err: unknown) => {
        if (err instanceof SsrRenderQueueFullError) {
          res.setHeader('Retry-After', '10');
          res.status(503).type('text/plain').send('SSR temporarily overloaded. Please retry later.');
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
    console.log(`SSR API internal origin: ${apiInternalOrigin}`);
    console.log(`SSR public page cache: ${pageCacheTtlSeconds}s / ${pageCacheMaxEntries} entries`);
    console.log(`SSR SEO document cache: ${seoDocumentCacheTtlSeconds}s / ${seoDocumentCacheMaxEntries} entries`);
    console.log(`SSR render enabled: ${ssrRenderEnabled}`);
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


function serveCsrFallbackPage(req: Request, res: Response, browserDistFolder: string): void {
  if (isExplicitNotFoundRoute(req.originalUrl)) {
    res.status(404);
  }

  res.setHeader('X-AmusementPark-SSR-Mode', 'CSR-FALLBACK');
  res.setHeader('Cache-Control', 'no-store');
  res.sendFile(join(browserDistFolder, 'index.html'));
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
  const acceptLanguage = req.headers['accept-language'] ?? '';

  return `${protocol}://${host}${req.originalUrl}::${acceptLanguage}`;
}

function isCacheablePageRequest(req: Request): boolean {
  if (req.method !== 'GET') {
    return false;
  }

  if (req.headers.authorization || req.headers.cookie) {
    return false;
  }

  if (!isPublicSsrCacheRoute(req.originalUrl)) {
    return false;
  }

  const acceptHeader = req.headers.accept ?? '';
  if (Array.isArray(acceptHeader)) {
    return acceptHeader.some((value: string) => value.includes('text/html'));
  }

  return acceptHeader.length === 0 || acceptHeader.includes('text/html') || acceptHeader.includes('*/*');
}

function isPublicSsrCacheRoute(url: string): boolean {
  const path = url.split(/[?#]/, 1)[0] ?? '';

  return /^\/?$/i.test(path)
    || /^\/[a-z]{2}\/?$/i.test(path)
    || /^\/[a-z]{2}\/parks\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+(?:\/items)?\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/?$/i.test(path)
    || /^\/[a-z]{2}\/park-(?:operator|founder|manufacturer)\/[^/]+\/[^/]+\/?$/i.test(path);
}

function getCachedPage(cacheKey: string): PageCacheEntry | null {
  const entry = pageCache.get(cacheKey);
  if (!entry) {
    return null;
  }

  if (entry.expiresAt <= Date.now()) {
    pageCache.delete(cacheKey);
    return null;
  }

  return entry;
}

function setCachedPage(cacheKey: string, statusCode: number, html: string): void {
  if (Buffer.byteLength(html, 'utf8') > 1024 * 1024) {
    return;
  }

  pageCache.set(cacheKey, {
    statusCode,
    html,
    expiresAt: Date.now() + pageCacheTtlSeconds * 1000
  });

  while (pageCache.size > pageCacheMaxEntries) {
    const oldestKey = pageCache.keys().next().value as string | undefined;
    if (!oldestKey) {
      break;
    }

    pageCache.delete(oldestKey);
  }
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
    joinCspDirective('script-src', ["'self'", "'unsafe-inline'", 'https://accounts.google.com', 'https://apis.google.com', 'https://matomo.cedric-caudron.com', ...localScriptSources]),
    joinCspDirective('style-src', ["'self'", "'unsafe-inline'", 'https://fonts.googleapis.com', 'https://accounts.google.com']),
    joinCspDirective('style-src-elem', ["'self'", "'unsafe-inline'", 'https://fonts.googleapis.com', 'https://accounts.google.com']),
    joinCspDirective('font-src', ["'self'", 'data:', 'https://fonts.gstatic.com']),
    joinCspDirective('img-src', ["'self'", 'data:', 'blob:', 'https:', 'https://tile.openstreetmap.org', 'https://*.tile.openstreetmap.org', ...localImageSources]),
    joinCspDirective('connect-src', ["'self'", 'https://accounts.google.com', 'https://www.googleapis.com', 'https://matomo.cedric-caudron.com', ...localConnectSources]),
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
  if (!isSeoDocumentCacheEnabled() || !isSeoDocumentCacheableMethod(req.method)) {
    proxyToApi(req, res, next, targetPath);
    return;
  }

  const cacheKey = buildSeoDocumentCacheKey(targetPath);
  const cachedEntry = getCachedSeoDocument(cacheKey);

  if (cachedEntry !== null) {
    writeCachedSeoDocument(req, res, cachedEntry, 'HIT');
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
  return seoDocumentCacheTtlSeconds > 0 && seoDocumentCacheMaxEntries > 0;
}

function isSeoDocumentCacheableMethod(method: string): boolean {
  return method.toUpperCase() === 'GET' || method.toUpperCase() === 'HEAD';
}

function buildSeoDocumentCacheKey(targetPath: string): string {
  return targetPath.toLowerCase();
}

function getCachedSeoDocument(cacheKey: string): SeoDocumentCacheEntry | null {
  const entry = seoDocumentCache.get(cacheKey);
  if (!entry) {
    return null;
  }

  if (entry.expiresAt <= Date.now()) {
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
          expiresAt: Date.now() + seoDocumentCacheTtlSeconds * 1000
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

    // Exclure le cache-control de l'API : on impose le nôtre ci-dessous pour
    // garantir une valeur cohérente indépendante de ce que retourne le OutputCache ASP.NET.
    // Exclure également le header Vary émis par OutputCache (Host, X-Forwarded-*)
    // qui peut perturber certains crawlers ou CDN intermédiaires.
    const normalizedName = name.toLowerCase();
    if (normalizedName === 'cache-control' || normalizedName === 'vary') {
      return;
    }

    responseHeaders[name] = value;
  });

  responseHeaders['content-length'] = bodyLength.toString();

  const browserMaxAge = Math.min(seoDocumentCacheTtlSeconds, 300);
  responseHeaders['cache-control'] = `public, max-age=${browserMaxAge}, stale-while-revalidate=60`;

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

/**
 * Construit les headers minimalistes pour le fetch interne d'un document SEO vers l'API.
 *
 * Contrairement à buildApiProxyHeaders qui transmet les headers de la requête entrante,
 * cette fonction produit un jeu de headers propre et déterministe :
 * - Pas de Cookie ni Authorization (garantit que le OutputCache API met en cache la réponse).
 * - Pas d'User-Agent du crawler entrant (évite toute logique basée sur l'UA côté API).
 * - Pas d'Accept-Encoding (le corps doit rester non compressé pour être mis en cache en mémoire).
 * - Accept: application/xml, text/plain pour guider la négociation de contenu.
 *
 * Les headers de forwarding (Host, X-Forwarded-*) sont conservés pour que l'API
 * construise correctement les URLs publiques (sitemap index, robots Sitemap:).
 */
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

function isExplicitNotFoundRoute(url: string): boolean {
  return /^\/[a-z]{2}\/not-found(?:[/?#]|$)/i.test(url);
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
