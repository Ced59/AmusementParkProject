#!/usr/bin/env node

const baseUrl = normalizeBaseUrl(process.env.PUBLIC_BASE_URL ?? process.argv[2] ?? 'http://localhost:4000');
const paths = (process.env.SEO_SMOKE_PATHS ?? '/en/home,/en/parks,/fr/parks,/en/about,/en/privacy')
  .split(',')
  .map((path) => path.trim())
  .filter(Boolean);

let failed = false;

for (const path of paths) {
  const url = new URL(path, baseUrl).toString();
  const response = await fetch(url, { redirect: 'manual' });
  const html = await response.text();
  const title = matchContent(html, /<title[^>]*>([\s\S]*?)<\/title>/i);
  const description = matchContent(html, /<meta\s+name=["']description["']\s+content=["']([^"']*)["'][^>]*>/i)
    ?? matchContent(html, /<meta\s+content=["']([^"']*)["']\s+name=["']description["'][^>]*>/i);
  const canonical = matchContent(html, /<link\s+rel=["']canonical["']\s+href=["']([^"']*)["'][^>]*>/i)
    ?? matchContent(html, /<link\s+href=["']([^"']*)["']\s+rel=["']canonical["'][^>]*>/i);
  const hasAppRootContent = /<app-root[^>]*>[\s\S]*?<\/[a-z0-9-]+>/i.test(html)
    && !/<app-root[^>]*>\s*<\/app-root>/i.test(html);

  const checks = [
    [response.status >= 200 && response.status < 400, `HTTP ${response.status}`],
    [Boolean(title && title.length >= 8), 'title'],
    [Boolean(description && description.length >= 30), 'meta description'],
    [Boolean(canonical && canonical.startsWith(baseUrl.origin)), 'canonical'],
    [hasAppRootContent, 'SSR app-root content']
  ];

  const failedChecks = checks.filter(([ok]) => !ok).map(([, label]) => label);

  if (failedChecks.length > 0) {
    failed = true;
    console.error(`✗ ${url}`);
    console.error(`  Missing/invalid: ${failedChecks.join(', ')}`);
    continue;
  }

  console.log(`✓ ${url}`);
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

function decodeHtml(value) {
  return value
    .replaceAll('&amp;', '&')
    .replaceAll('&quot;', '"')
    .replaceAll('&#39;', "'")
    .replaceAll('&lt;', '<')
    .replaceAll('&gt;', '>');
}
