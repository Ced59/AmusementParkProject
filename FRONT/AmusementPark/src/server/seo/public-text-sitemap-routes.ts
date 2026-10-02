import express from 'express';
import type { Express } from 'express';

/** Bounded fetch control independent of the XML snapshot and API proxy. */
export function registerPublicTextSitemapRoutes(server: Express, browserDirectory: string): void {
  server.get('/sitemap-static-fr.txt', express.static(browserDirectory, {
    index: false,
    fallthrough: false,
    setHeaders(response): void {
      response.setHeader('Content-Type', 'text/plain; charset=utf-8');
      response.setHeader('Cache-Control', 'no-cache, max-age=0, must-revalidate');
      response.setHeader('X-Accel-Buffering', 'no');
    }
  }));
}
