export interface RobotHtmlOptimizationResult {
  readonly html: string;
  readonly removedScriptCount: number;
  readonly removedScriptLikeLinkCount: number;
}

export function optimizeHtmlForRobotNoJs(html: string): RobotHtmlOptimizationResult {
  const scriptResult: ScriptStripResult = stripExecutableScripts(html);
  const linkResult: ScriptLikeLinkStripResult = stripScriptLikeLinks(scriptResult.html);

  return {
    html: linkResult.html,
    removedScriptCount: scriptResult.removedScriptCount,
    removedScriptLikeLinkCount: linkResult.removedScriptLikeLinkCount
  };
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
