import { inspectSeoReadyHtml } from './robot-html-optimizer';
import type { SeoReadyHtmlReason } from './robot-html-optimizer';

export interface SsrPageCacheHtmlDecision {
  readonly canCache: boolean;
  readonly reason: SeoReadyHtmlReason;
  readonly bodyTextLength: number;
}

export function shouldCacheSsrRenderedHtml(html: string): SsrPageCacheHtmlDecision {
  const seoReady = inspectSeoReadyHtml(html);

  return {
    canCache: seoReady.isReady,
    reason: seoReady.reason,
    bodyTextLength: seoReady.bodyTextLength
  };
}
