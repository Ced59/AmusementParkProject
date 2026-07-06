export interface RobotHtmlOptimizationResult {
  readonly html: string;
  readonly removedScriptCount: number;
  readonly removedScriptLikeLinkCount: number;
}

export type SeoReadyHtmlReason =
  'ready'
  | 'bare-angular-shell'
  | 'missing-title'
  | 'missing-meta-description'
  | 'missing-canonical'
  | 'insufficient-body-content';

export interface SeoReadyHtmlCheckResult {
  readonly isReady: boolean;
  readonly reason: SeoReadyHtmlReason;
  readonly bodyTextLength: number;
}

export type RobotHtmlPreparationStatus =
  'no-js'
  | 'blocked-not-seo-ready'
  | 'disabled'
  | 'not-allowed';

export interface RobotHtmlPreparationOptions {
  readonly allowRobotNoJsOptimization: boolean;
  readonly robotNoJsHtmlEnabled: boolean;
  readonly isRobotRequest: boolean;
}

export interface RobotHtmlPreparationResult {
  readonly html: string;
  readonly seoReady: SeoReadyHtmlCheckResult;
  readonly robotHtmlStatus: RobotHtmlPreparationStatus | null;
  readonly removedScriptCount: number;
  readonly removedScriptLikeLinkCount: number;
}

const minimumSeoReadyBodyTextLength = 500;

export function optimizeHtmlForRobotNoJs(html: string): RobotHtmlOptimizationResult {
  const scriptResult: ScriptStripResult = stripExecutableScripts(html);
  const linkResult: ScriptLikeLinkStripResult = stripScriptLikeLinks(scriptResult.html);
  const compactHtml: string = compactPresentationalRobotHtml(linkResult.html);

  return {
    html: compactHtml,
    removedScriptCount: scriptResult.removedScriptCount,
    removedScriptLikeLinkCount: linkResult.removedScriptLikeLinkCount
  };
}

export function prepareRobotHtmlForResponse(html: string, options: RobotHtmlPreparationOptions): RobotHtmlPreparationResult {
  const seoReady: SeoReadyHtmlCheckResult = inspectSeoReadyHtml(html);

  if (!options.isRobotRequest) {
    return createPreparationResult(html, seoReady, null, 0, 0);
  }

  if (!options.allowRobotNoJsOptimization) {
    return createPreparationResult(html, seoReady, 'not-allowed', 0, 0);
  }

  if (!options.robotNoJsHtmlEnabled) {
    return createPreparationResult(html, seoReady, 'disabled', 0, 0);
  }

  if (!seoReady.isReady) {
    return createPreparationResult(html, seoReady, 'blocked-not-seo-ready', 0, 0);
  }

  const optimizationResult: RobotHtmlOptimizationResult = optimizeHtmlForRobotNoJs(html);

  return createPreparationResult(
    optimizationResult.html,
    seoReady,
    'no-js',
    optimizationResult.removedScriptCount,
    optimizationResult.removedScriptLikeLinkCount
  );
}

export function shouldReturnBotSsrUnavailable(isRobotRequest: boolean, statusCode: number): boolean {
  return isRobotRequest && statusCode === 200;
}

export function isSeoReadyHtml(html: string): boolean {
  return inspectSeoReadyHtml(html).isReady;
}

export function inspectSeoReadyHtml(html: string): SeoReadyHtmlCheckResult {
  const bodyTextLength: number = getMeaningfulBodyTextLength(html);

  if (isBareAngularShell(html)) {
    return { isReady: false, reason: 'bare-angular-shell', bodyTextLength };
  }

  if (!hasNonEmptyTitle(html)) {
    return { isReady: false, reason: 'missing-title', bodyTextLength };
  }

  if (!hasMetaDescription(html)) {
    return { isReady: false, reason: 'missing-meta-description', bodyTextLength };
  }

  if (!hasCanonical(html)) {
    return { isReady: false, reason: 'missing-canonical', bodyTextLength };
  }

  if (bodyTextLength < minimumSeoReadyBodyTextLength) {
    return { isReady: false, reason: 'insufficient-body-content', bodyTextLength };
  }

  return { isReady: true, reason: 'ready', bodyTextLength };
}

export function isBareAngularShell(html: string): boolean {
  return /<app-root\b[^>]*>\s*<\/app-root>/i.test(html)
    || /<app-root\b[^>]*>\s*<\/body>/i.test(html);
}

function createPreparationResult(
  html: string,
  seoReady: SeoReadyHtmlCheckResult,
  robotHtmlStatus: RobotHtmlPreparationStatus | null,
  removedScriptCount: number,
  removedScriptLikeLinkCount: number
): RobotHtmlPreparationResult {
  return {
    html,
    seoReady,
    robotHtmlStatus,
    removedScriptCount,
    removedScriptLikeLinkCount
  };
}

function hasNonEmptyTitle(html: string): boolean {
  const match: RegExpExecArray | null = /<title\b[^>]*>([\s\S]*?)<\/title>/i.exec(html);
  const title: string = match?.[1]
    .replace(/<[^>]+>/g, ' ')
    .replace(/\s+/g, ' ')
    .trim() ?? '';

  return title.length > 0;
}

function hasMetaDescription(html: string): boolean {
  const metaTags: RegExpMatchArray | null = html.match(/<meta\b[^>]*>/gi);

  return (metaTags ?? []).some((tag: string): boolean => {
    return getHtmlAttributeValue(tag, 'name').toLowerCase() === 'description'
      && getHtmlAttributeValue(tag, 'content').trim().length > 0;
  });
}

function hasCanonical(html: string): boolean {
  const linkTags: RegExpMatchArray | null = html.match(/<link\b[^>]*>/gi);

  return (linkTags ?? []).some((tag: string): boolean => {
    const relValues: ReadonlySet<string> = new Set<string>(
      getHtmlAttributeValue(tag, 'rel')
        .toLowerCase()
        .split(/\s+/)
        .filter((value: string): boolean => value.length > 0)
    );

    return relValues.has('canonical') && getHtmlAttributeValue(tag, 'href').trim().length > 0;
  });
}

function getMeaningfulBodyTextLength(html: string): number {
  const bodyMatch: RegExpExecArray | null = /<body\b[^>]*>([\s\S]*?)<\/body>/i.exec(html);
  const bodyHtml: string = bodyMatch?.[1] ?? html;
  const bodyText: string = bodyHtml
    .replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi, ' ')
    .replace(/<style\b[^>]*>[\s\S]*?<\/style>/gi, ' ')
    .replace(/<noscript\b[^>]*>[\s\S]*?<\/noscript>/gi, ' ')
    .replace(/<[^>]+>/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();

  return bodyText.length;
}

interface ScriptStripResult {
  readonly html: string;
  readonly removedScriptCount: number;
}

interface ScriptLikeLinkStripResult {
  readonly html: string;
  readonly removedScriptLikeLinkCount: number;
}

function stripExecutableScripts(html: string): ScriptStripResult {
  let removedScriptCount: number = 0;
  const optimizedHtml: string = html.replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi, (scriptTag: string): string => {
    if (isJsonLdScriptTag(scriptTag)) {
      return scriptTag;
    }

    removedScriptCount += 1;
    return '';
  });

  return {
    html: optimizedHtml,
    removedScriptCount
  };
}

function stripScriptLikeLinks(html: string): ScriptLikeLinkStripResult {
  let removedScriptLikeLinkCount: number = 0;
  const optimizedHtml: string = html.replace(/<link\b[^>]*>/gi, (linkTag: string): string => {
    if (!isScriptLikeLinkTag(linkTag)) {
      return linkTag;
    }

    removedScriptLikeLinkCount += 1;
    return '';
  });

  return {
    html: optimizedHtml,
    removedScriptLikeLinkCount
  };
}

function compactPresentationalRobotHtml(html: string): string {
  let optimizedHtml: string = html
    .replace(/<style\b[^>]*>[\s\S]*?<\/style>/gi, '')
    .replace(/<noscript\b[^>]*>[\s\S]*?<\/noscript>/gi, '')
    .replace(/<!---->/g, '')
    .replace(/\s(?:_ngcontent|_nghost)-[^\s=>]+(?:=(?:"[^"]*"|'[^']*'))?/gi, '')
    .replace(/\s(?:ngh|ng-server-context|ng-version)=(?:"[^"]*"|'[^']*')/gi, '')
    .replace(/\s(?:class|style|role|tabindex|aria-hidden|width|height|loading|decoding|fetchpriority)=(?:"[^"]*"|'[^']*')/gi, '')
    .replace(/<i\b[^>]*>\s*<\/i>/gi, '')
    .replace(/<svg\b[^>]*>[\s\S]*?<\/svg>/gi, '');

  for (let pass: number = 0; pass < 6; pass += 1) {
    optimizedHtml = optimizedHtml.replace(/<([a-z][a-z0-9-]*)\b[^>]*>\s*<\/\1>/gi, '');
  }

  return optimizedHtml.replace(/>\s+</g, '><');
}

function isJsonLdScriptTag(scriptTag: string): boolean {
  const type: string = getHtmlAttributeValue(scriptTag, 'type').toLowerCase();

  return type === 'application/ld+json';
}

function isScriptLikeLinkTag(linkTag: string): boolean {
  const relValues: ReadonlySet<string> = new Set<string>(
    getHtmlAttributeValue(linkTag, 'rel')
      .toLowerCase()
      .split(/\s+/)
      .filter((value: string): boolean => value.length > 0)
  );

  if (relValues.has('modulepreload')) {
    return true;
  }

  if (!relValues.has('preload') && !relValues.has('prefetch')) {
    return false;
  }

  const asValue: string = getHtmlAttributeValue(linkTag, 'as').toLowerCase();
  const hrefValue: string = getHtmlAttributeValue(linkTag, 'href').toLowerCase();

  return asValue === 'script' || /\.(?:js|mjs)(?:[?#].*)?$/.test(hrefValue);
}

function getHtmlAttributeValue(tag: string, attributeName: string): string {
  const escapedAttributeName: string = escapeRegex(attributeName);
  const pattern: RegExp = new RegExp(`\\s${escapedAttributeName}\\s*=\\s*(['"])(.*?)\\1`, 'i');
  const match: RegExpExecArray | null = pattern.exec(tag);

  return match?.[2] ?? '';
}

function escapeRegex(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
