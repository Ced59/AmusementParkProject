import { enforceNoindexHtml } from './robot-html-optimizer';
import { resolveXRobotsTagHeader } from './ssr-route-status.helpers';

export interface SsrRobotsResponse {
  readonly html: string;
  readonly directive: string | null;
}

export function prepareSsrRobotsResponse(html: string, url: string, statusCode: number, isCsrFallback: boolean): SsrRobotsResponse {
  const directive: string | null = resolveXRobotsTagHeader(url, statusCode, isCsrFallback);
  return {
    html: directive !== null ? enforceNoindexHtml(html, directive) : html,
    directive
  };
}
