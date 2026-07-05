#!/usr/bin/env node

const baseUrl = normalizeBaseUrl(process.env.PUBLIC_BASE_URL ?? process.argv[2] ?? 'http://localhost:4000');
const paths = (process.env.SEO_SMOKE_PATHS ?? '/en/home,/en/parks,/fr/parks,/en/about,/en/privacy')
  .split(',')
  .map((path) => path.trim())
  .filter(Boolean);
const userAgent = process.env.SEO_SMOKE_USER_AGENT
  ?? 'Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)';
const minimumBodyTextLength = normalizePositiveInteger(process.env.SEO_SMOKE_MIN_BODY_TEXT_LENGTH, 500);

let failed = false;

for (const path of paths) {
  const url = new URL(path, baseUrl).toString();
  const response = await fetch(url, {
    redirect: 'manual',
    headers: {
      accept: 'text/html',
      'user-agent': userAgent
    }
  });
  const html = await response.text();
  const title = matchContent(html, /<title[^>]*>([\s\S]*?)<\/title>/i);
  const description = findMetaContent(html, 'description');
  const canonical = findCanonicalHref(html);
  const hasAppRootContent = /<app-root[^>]*>[\s\S]*?<\/[a-z0-9-]+>/i.test(html)
    && !/<app-root[^>]*>\s*<\/app-root>/i.test(html);
  const bodyTextLength = getBodyTextLength(html);
  const executableScriptCount = countExecutableScripts(html);
  const scriptLikeLinkCount = countScriptLikeLinks(html);

  const checks = [
    [response.status >= 200 && response.status < 400, `HTTP ${response.status}`],
    [Boolean(title && title.length >= 8), 'title'],
    [Boolean(description && description.length >= 30), 'meta description'],
    [Boolean(canonical && canonical.startsWith(baseUrl.origin)), 'canonical'],
    [hasAppRootContent, 'SSR app-root content'],
    [bodyTextLength >= minimumBodyTextLength, `body text length ${bodyTextLength}`],
    [executableScriptCount === 0, `${executableScriptCount} executable script(s)`],
    [scriptLikeLinkCount === 0, `${scriptLikeLinkCount} script preload/prefetch link(s)`]
  ];

  const failedChecks = checks.filter(([ok]) => !ok).map(([, label]) => label);

  if (failedChecks.length > 0) {
    failed = true;
    console.error(`FAIL ${url}`);
    console.error(`  Missing/invalid: ${failedChecks.join(', ')}`);
    continue;
  }

  console.log(`OK ${url} (${bodyTextLength} body chars, no executable JS)`);
}

if (failed) {
  process.exitCode = 1;
}

function normalizeBaseUrl(value) {
  const url = new URL(value.endsWith('/') ? value : `${value}/`);
  return url;
}

function matchContent(value, regex) {
  const match = regex.exec(value);

  if (!match) {
    return null;
  }

  return decodeHtml(match[1].trim());
}

function findMetaContent(html, name) {
  const normalizedName = name.toLowerCase();
  const tag = (html.match(/<meta\b[^>]*>/gi) ?? [])
    .find((candidate) => getAttributeValue(candidate, 'name').toLowerCase() === normalizedName);

  return tag ? decodeHtml(getAttributeValue(tag, 'content').trim()) : null;
}

function findCanonicalHref(html) {
  const tag = (html.match(/<link\b[^>]*>/gi) ?? [])
    .find((candidate) => {
      const relValues = new Set(getAttributeValue(candidate, 'rel').toLowerCase().split(/\s+/).filter(Boolean));
      return relValues.has('canonical');
    });

  return tag ? decodeHtml(getAttributeValue(tag, 'href').trim()) : null;
}

function getBodyTextLength(html) {
  const body = /<body\b[^>]*>([\s\S]*?)<\/body>/i.exec(html)?.[1] ?? html;
  return decodeHtml(body
    .replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi, ' ')
    .replace(/<style\b[^>]*>[\s\S]*?<\/style>/gi, ' ')
    .replace(/<noscript\b[^>]*>[\s\S]*?<\/noscript>/gi, ' ')
    .replace(/<[^>]+>/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()).length;
}

function countExecutableScripts(html) {
  return (html.match(/<script\b[^>]*>/gi) ?? [])
    .filter((scriptTag) => {
      const type = getAttributeValue(scriptTag, 'type').toLowerCase();
      return type !== 'application/ld+json';
    })
    .length;
}

function countScriptLikeLinks(html) {
  return (html.match(/<link\b[^>]*>/gi) ?? [])
    .filter((linkTag) => {
      const relValues = new Set(getAttributeValue(linkTag, 'rel').toLowerCase().split(/\s+/).filter(Boolean));

      if (relValues.has('modulepreload')) {
        return true;
      }

      if (!relValues.has('preload') && !relValues.has('prefetch')) {
        return false;
      }

      const asValue = getAttributeValue(linkTag, 'as').toLowerCase();
      const hrefValue = getAttributeValue(linkTag, 'href').toLowerCase();
      return asValue === 'script' || /\.(?:js|mjs)(?:[?#].*)?$/.test(hrefValue);
    })
    .length;
}

function getAttributeValue(tag, attributeName) {
  const escapedAttributeName = attributeName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const match = new RegExp(`\\s${escapedAttributeName}\\s*=\\s*(['"])(.*?)\\1`, 'i').exec(tag);
  return match?.[2] ?? '';
}

function normalizePositiveInteger(value, fallback) {
  const parsed = Number.parseInt(value ?? '', 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

function decodeHtml(value) {
  return value
    .replaceAll('&amp;', '&')
    .replaceAll('&quot;', '"')
    .replaceAll('&#39;', "'")
    .replaceAll('&lt;', '<')
    .replaceAll('&gt;', '>')
    .replaceAll('&nbsp;', ' ');
}
