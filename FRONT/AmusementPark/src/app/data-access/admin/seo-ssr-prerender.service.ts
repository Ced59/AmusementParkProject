import { Injectable } from '@angular/core';

import { SeoSsrPrerenderProgress, SeoSsrPrerenderResult } from '@app/models/admin/seo/seo-sitemap.models';

interface SitemapCollectionResult {
  readonly sitemapUrls: string[];
  readonly pageUrls: string[];
}

/**
 * Réchauffe le cache HTML SSR en partant du sitemap index public.
 *
 * Le service s'exécute depuis l'admin navigateur : l'autorisation reste celle
 * de la page admin, tandis que les requêtes de warmup vers les pages publiques
 * sont faites sans credentials pour ne pas injecter de cookies Matomo/auth dans
 * le cache HTML public.
 */
@Injectable({ providedIn: 'root' })
export class SeoSsrPrerenderService {
  private static readonly WarmupHeaderName: string = 'X-AmusementPark-SSR-Warmup';
  private static readonly WarmupConcurrency: number = 1;

  async prerenderFromSitemapIndex(
    sitemapIndexUrl: string,
    progressCallback: (progress: SeoSsrPrerenderProgress) => void
  ): Promise<SeoSsrPrerenderResult> {
    const startedAt: number = Date.now();
    const collected: SitemapCollectionResult = await this.collectSitemapUrls(sitemapIndexUrl);
    const uniquePageUrls: string[] = Array.from(new Set(collected.pageUrls));
    let processedUrlCount: number = 0;
    let succeededUrlCount: number = 0;
    let failedUrlCount: number = 0;
    const errors: string[] = [];

    this.emitProgress(progressCallback, {
      status: 'Running',
      totalUrlCount: uniquePageUrls.length,
      processedUrlCount,
      succeededUrlCount,
      failedUrlCount,
      currentUrl: null,
      errors: []
    });

    let nextIndex: number = 0;
    const workers: Promise<void>[] = Array.from({ length: SeoSsrPrerenderService.WarmupConcurrency }, async (): Promise<void> => {
      while (nextIndex < uniquePageUrls.length) {
        const currentIndex: number = nextIndex;
        nextIndex += 1;
        const pageUrl: string = uniquePageUrls[currentIndex]!;

        this.emitProgress(progressCallback, {
          status: 'Running',
          totalUrlCount: uniquePageUrls.length,
          processedUrlCount,
          succeededUrlCount,
          failedUrlCount,
          currentUrl: pageUrl,
          errors
        });

        try {
          await this.warmupPage(pageUrl);
          succeededUrlCount += 1;
        } catch (error: unknown) {
          failedUrlCount += 1;
          errors.push(this.formatWarmupError(pageUrl, error));
        } finally {
          processedUrlCount += 1;
          this.emitProgress(progressCallback, {
            status: 'Running',
            totalUrlCount: uniquePageUrls.length,
            processedUrlCount,
            succeededUrlCount,
            failedUrlCount,
            currentUrl: pageUrl,
            errors
          });
        }
      }
    });

    await Promise.all(workers);

    const result: SeoSsrPrerenderResult = {
      status: failedUrlCount === 0 ? 'Succeeded' : 'Failed',
      startedAtUtc: new Date(startedAt).toISOString(),
      completedAtUtc: new Date().toISOString(),
      durationMs: Date.now() - startedAt,
      totalUrlCount: uniquePageUrls.length,
      processedUrlCount,
      succeededUrlCount,
      failedUrlCount,
      errors
    };

    this.emitProgress(progressCallback, {
      status: result.status,
      totalUrlCount: result.totalUrlCount,
      processedUrlCount: result.processedUrlCount,
      succeededUrlCount: result.succeededUrlCount,
      failedUrlCount: result.failedUrlCount,
      currentUrl: null,
      errors: result.errors
    });

    return result;
  }

  private async collectSitemapUrls(sitemapIndexUrl: string): Promise<SitemapCollectionResult> {
    const sitemapUrls: string[] = [];
    const pageUrls: string[] = [];
    const pendingSitemapUrls: string[] = [sitemapIndexUrl];
    const visitedSitemapUrls: Set<string> = new Set<string>();

    while (pendingSitemapUrls.length > 0) {
      const sitemapUrl: string = pendingSitemapUrls.shift()!;
      if (visitedSitemapUrls.has(sitemapUrl)) {
        continue;
      }

      visitedSitemapUrls.add(sitemapUrl);
      sitemapUrls.push(sitemapUrl);

      const xml: string = await this.fetchText(sitemapUrl);
      const document: Document = new DOMParser().parseFromString(xml, 'application/xml');
      const nestedSitemapUrls: string[] = this.readLocValues(document, 'sitemap > loc');

      if (nestedSitemapUrls.length > 0) {
        nestedSitemapUrls.forEach((url: string): void => {
          if (!visitedSitemapUrls.has(url)) {
            pendingSitemapUrls.push(url);
          }
        });
        continue;
      }

      pageUrls.push(...this.readLocValues(document, 'url > loc'));
    }

    return { sitemapUrls, pageUrls };
  }

  private async fetchText(url: string): Promise<string> {
    const response: Response = await fetch(this.normalizeWarmupUrl(url), {
      method: 'GET',
      credentials: 'omit',
      cache: 'no-store',
      headers: {
        Accept: 'application/xml,text/xml,text/plain,*/*'
      }
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

    return response.text();
  }

  private async warmupPage(url: string): Promise<void> {
    const response: Response = await fetch(this.normalizeWarmupUrl(url), {
      method: 'GET',
      credentials: 'omit',
      cache: 'no-store',
      headers: {
        Accept: 'text/html,*/*',
        [SeoSsrPrerenderService.WarmupHeaderName]: '1'
      }
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }
  }

  private readLocValues(document: Document, selector: string): string[] {
    return Array.from(document.querySelectorAll(selector))
      .map((element: Element): string => element.textContent?.trim() ?? '')
      .filter((value: string): boolean => value.length > 0);
  }

  private normalizeWarmupUrl(value: string): string {
    const parsedUrl: URL = new URL(value, window.location.origin);

    if (parsedUrl.origin === window.location.origin) {
      return `${parsedUrl.pathname}${parsedUrl.search}${parsedUrl.hash}`;
    }

    return parsedUrl.toString();
  }

  private formatWarmupError(url: string, error: unknown): string {
    if (error instanceof Error) {
      return `${url} — ${error.message}`;
    }

    return `${url} — unknown error`;
  }

  private emitProgress(progressCallback: (progress: SeoSsrPrerenderProgress) => void, progress: SeoSsrPrerenderProgress): void {
    progressCallback({
      ...progress,
      errors: progress.errors.slice(-10)
    });
  }
}
