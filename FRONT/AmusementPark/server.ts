import { APP_BASE_HREF } from '@angular/common';
import { CommonEngine } from '@angular/ssr/node';
import express, { NextFunction, Request, Response } from 'express';
import http from 'node:http';
import https from 'node:https';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import AppServerModule from './src/main.server';
import { SSR_RESPONSE } from './src/app/core/ssr/ssr-response.token';

const defaultApiInternalOrigin = 'http://api:8080';
const apiInternalOrigin = normalizeOrigin(process.env['SSR_API_INTERNAL_URL'] ?? defaultApiInternalOrigin);
const cspReportOnly = (process.env['SSR_CSP_REPORT_ONLY'] ?? 'true').toLowerCase() !== 'false';
const cspEnabled = (process.env['SSR_CSP_ENABLED'] ?? 'true').toLowerCase() !== 'false';
const ssrAllowedHosts = splitConfiguredValues(process.env['SSR_ALLOWED_HOSTS'] ?? 'localhost;127.0.0.1;amusement.localhost;front');

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

  server.use(applySecurityHeaders);

  server.get('/healthz', (_req: Request, res: Response) => {
    res.status(200).type('text/plain').send('ok\n');
  });

  server.get('/robots.txt', (req: Request, res: Response, next: NextFunction) => {
    proxyToApi(req, res, next, req.originalUrl);
  });

  server.get('/sitemap.xml', (req: Request, res: Response, next: NextFunction) => {
    proxyToApi(req, res, next, req.originalUrl);
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
    const publicUrl = getPublicRequestUrl(req);

    if (isExplicitNotFoundRoute(req.originalUrl)) {
      res.status(404);
    }

    commonEngine
      .render({
        bootstrap: AppServerModule,
        documentFilePath: indexHtml,
        url: publicUrl,
        publicPath: browserDistFolder,
        providers: [
          { provide: APP_BASE_HREF, useValue: req.baseUrl },
          { provide: SSR_RESPONSE, useValue: res }
        ],
      })
      .then((html: string) => res.send(html))
      .catch((err: unknown) => next(err));
  });

  return server;
}

function run(): void {
  const port = Number(process.env['PORT'] ?? 4000);
  const server = app();

  server.listen(port, () => {
    console.log(`Angular SSR server listening on http://0.0.0.0:${port}`);
    console.log(`SSR API internal origin: ${apiInternalOrigin}`);
  });
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

  return [
    "default-src 'self'",
    "base-uri 'self'",
    "object-src 'none'",
    "frame-ancestors 'none'",
    "form-action 'self'",
    "script-src 'self' 'unsafe-inline' https://accounts.google.com https://apis.google.com https://matomo.cedric-caudron.com",
    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://accounts.google.com",
    "style-src-elem 'self' 'unsafe-inline' https://fonts.googleapis.com https://accounts.google.com",
    "font-src 'self' data: https://fonts.gstatic.com",
    "img-src 'self' data: blob: https: http://localhost:* http://amusement.localhost:* http://matomo.amusement.localhost:*",
    "connect-src 'self' http://localhost:* https://localhost:* http://amusement.localhost:* http://matomo.amusement.localhost:* https://accounts.google.com https://www.googleapis.com https://matomo.cedric-caudron.com",
    "frame-src 'self' https://accounts.google.com",
    "worker-src 'self' blob:",
    "media-src 'self' blob: data:",
    "manifest-src 'self'",
    `report-uri ${reportUri}`
  ].join('; ');
}

function proxyToApi(req: Request, res: Response, next: NextFunction, targetPath: string): void {
  const targetUrl = new URL(targetPath, apiInternalOrigin);
  const client = targetUrl.protocol === 'https:' ? https : http;
  const publicProtocol = getForwardedValue(req, 'x-forwarded-proto') ?? req.protocol;
  const publicHost = getForwardedValue(req, 'x-forwarded-host') ?? req.headers.host ?? targetUrl.host;

  const headers: http.OutgoingHttpHeaders = { ...req.headers };
  delete headers['host'];
  headers['host'] = publicHost;
  headers['x-forwarded-host'] = publicHost;
  headers['x-forwarded-proto'] = publicProtocol;
  headers['x-forwarded-for'] = appendForwardedFor(req);

  if (req.originalUrl.toLowerCase().startsWith('/api')) {
    headers['x-forwarded-prefix'] = '/api';
  }

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
