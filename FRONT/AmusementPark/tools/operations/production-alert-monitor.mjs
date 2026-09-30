import { appendFile, readFile, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

import { runPerformanceBaseline } from '../performance/performance-baseline.mjs';

const toolDirectory = dirname(fileURLToPath(import.meta.url));
const defaultConfigPath = resolve(toolDirectory, 'production-alerts.config.json');
const maximumClientAssets = 25;
const clientAssetConcurrency = 4;

async function fetchTextWithTimeout(fetchImplementation, url, options, timeoutMilliseconds) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMilliseconds);
  try {
    const response = await fetchImplementation(url, { ...options, signal: controller.signal });
    const body = await response.text();
    return { response, body };
  } finally {
    clearTimeout(timeout);
  }
}

function readAttribute(tag, name) {
  const match = tag.match(new RegExp(`\\b${name}\\s*=\\s*["']([^"']+)["']`, 'i'));
  return match?.[1] ?? null;
}

export function extractRequiredClientAssetUrls(pageUrl, html) {
  const page = new URL(pageUrl);
  const tags = html.match(/<(?:script|link)\b[^>]*>/gi) ?? [];
  const baseTag = html.match(/<base\b[^>]*>/i)?.[0];
  const documentBase = new URL(baseTag ? readAttribute(baseTag, 'href') ?? page.href : page.href, page);
  const urls = [];

  for (const tag of tags) {
    const isScript = /^<script\b/i.test(tag);
    const relation = readAttribute(tag, 'rel')?.toLowerCase() ?? '';
    const reference = isScript ? readAttribute(tag, 'src') : readAttribute(tag, 'href');
    if (!reference || (!isScript && !relation.split(/\s+/).includes('modulepreload'))) {
      continue;
    }

    const asset = new URL(reference, documentBase);
    if (asset.origin === page.origin && !urls.includes(asset.href)) {
      urls.push(asset.href);
    }
  }

  return urls.slice(0, maximumClientAssets);
}

export function extractLazyRouteAssetUrl(assetUrl, source, routePath) {
  const escapedRoutePath = routePath.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const routePattern = new RegExp(
    `path\\s*:\\s*["']${escapedRoutePath}["'][\\s\\S]{0,300}?import\\s*\\(\\s*["']([^"']+\\.js)["']`,
  );
  const reference = source.match(routePattern)?.[1];
  if (!reference) {
    return null;
  }

  const resolved = new URL(reference, assetUrl);
  return resolved.origin === new URL(assetUrl).origin ? resolved.href : null;
}

async function probeJavaScriptAsset(assetUrl, fetchImplementation, timeoutMilliseconds) {
  try {
    const { response, body: source } = await fetchTextWithTimeout(fetchImplementation, assetUrl, {
      headers: { accept: 'text/javascript, application/javascript', 'user-agent': 'AmusementParkProductionMonitor/1.0' },
    }, timeoutMilliseconds);
    const contentType = response.headers.get('content-type')?.toLowerCase() ?? '';
    if (!response.ok || source.length === 0 || !contentType.includes('javascript')) {
      return { failure: `${new URL(assetUrl).pathname}: bundle client invalide`, source: null };
    }
    return { failure: null, source };
  } catch {
    return { failure: `${new URL(assetUrl).pathname}: bundle client inaccessible ou expiré`, source: null };
  }
}

async function probeJavaScriptAssets(assetUrls, fetchImplementation, timeoutMilliseconds) {
  const results = new Map();
  for (let index = 0; index < assetUrls.length; index += clientAssetConcurrency) {
    const batch = assetUrls.slice(index, index + clientAssetConcurrency);
    const batchResults = await Promise.all(batch.map(async (assetUrl) => ({
      assetUrl,
      result: await probeJavaScriptAsset(assetUrl, fetchImplementation, timeoutMilliseconds),
    })));
    for (const item of batchResults) {
      results.set(item.assetUrl, item.result);
    }
  }
  return results;
}

export async function probeRequiredClientAssets(
  baseUrl,
  target,
  fetchImplementation = globalThis.fetch,
  timeoutMilliseconds = 8000,
) {
  const pageUrl = new URL(target.path, baseUrl);
  try {
    const { response: pageResponse, body: pageBody } = await fetchTextWithTimeout(fetchImplementation, pageUrl, {
      headers: { accept: 'text/html', 'user-agent': 'AmusementParkProductionMonitor/1.0' },
    }, timeoutMilliseconds);
    if (pageResponse.status !== target.expectedStatus) {
      return { assetCount: 0, failures: [`page cliente HTTP ${pageResponse.status}`] };
    }

    const assetUrls = extractRequiredClientAssetUrls(pageUrl, pageBody);
    if (assetUrls.length === 0) {
      return { assetCount: 0, failures: ['aucun bundle client same-origin trouvé'] };
    }

    const failures = [];
    const sources = new Map();
    const assetResults = await probeJavaScriptAssets(assetUrls, fetchImplementation, timeoutMilliseconds);
    for (const [assetUrl, result] of assetResults) {
      if (result.failure) {
        failures.push(result.failure);
      } else {
        sources.set(assetUrl, result.source);
      }
    }

    let lazyRouteAssetUrl = null;
    if (target.clientRoutePath && failures.length === 0) {
      for (const [assetUrl, source] of sources) {
        lazyRouteAssetUrl = extractLazyRouteAssetUrl(assetUrl, source, target.clientRoutePath);
        if (lazyRouteAssetUrl) {
          break;
        }
      }

      if (!lazyRouteAssetUrl) {
        failures.push(`chunk dynamique de la route ${target.clientRoutePath} introuvable`);
      } else if (!sources.has(lazyRouteAssetUrl)) {
        const lazyResult = await probeJavaScriptAsset(lazyRouteAssetUrl, fetchImplementation, timeoutMilliseconds);
        if (lazyResult.failure) {
          failures.push(lazyResult.failure);
        }
      }
    }

    return {
      assetCount: assetUrls.length + (lazyRouteAssetUrl && !assetUrls.includes(lazyRouteAssetUrl) ? 1 : 0),
      lazyRouteAsset: lazyRouteAssetUrl ? new URL(lazyRouteAssetUrl).pathname : null,
      failures,
    };
  } catch {
    return {
      assetCount: 0,
      lazyRouteAsset: null,
      failures: ['page cliente inaccessible ou expirée pendant le contrôle des bundles'],
    };
  }
}

async function appendClientAssetChecks(report, config, baseUrl, fetchImplementation) {
  for (const target of config.baseline.targets.filter((candidate) => candidate.kind === 'csr-page')) {
    const result = report.results.find((candidate) => candidate.key === target.key);
    if (!result) {
      continue;
    }

    const check = await probeRequiredClientAssets(
      baseUrl,
      target,
      fetchImplementation,
      config.baseline.timeoutMilliseconds,
    );
    result.clientAssetCheck = check;
    result.failures.push(...check.failures);
  }
  return report;
}

function failedTargetKeys(report) {
  return new Set(report.results
    .filter((result) => result.failures.length > 0)
    .map((result) => result.key));
}

export function confirmIncidents(config, attempts) {
  if (attempts.length < config.confirmationAttempts) {
    return [];
  }

  const targets = new Map(config.baseline.targets.map((target) => [target.key, target]));
  const confirmedKeys = [...failedTargetKeys(attempts[0])]
    .filter((key) => attempts.every((attempt) => failedTargetKeys(attempt).has(key)));

  return confirmedKeys.map((key) => {
    const target = targets.get(key);
    return {
      targetKey: key,
      path: target.path,
      incidentId: target.incidentId,
      attempts: attempts.map((attempt) => {
        const result = attempt.results.find((candidate) => candidate.key === key);
        return {
          measuredAtUtc: attempt.measuredAtUtc,
          failures: result?.failures ?? [],
          statuses: result?.statuses ?? {},
          outcomes: result?.outcomes ?? {},
          p95Milliseconds: result?.p95Milliseconds ?? null,
        };
      }),
    };
  });
}

export async function runConfirmedProductionProbe(config, options = {}) {
  const runBaseline = options.runBaseline ?? runPerformanceBaseline;
  const pause = options.pause ?? ((milliseconds) => new Promise((resolvePromise) => setTimeout(resolvePromise, milliseconds)));
  const baseUrl = options.baseUrl ?? 'https://amusement-parks.fun';
  const attempts = [];

  for (let index = 0; index < config.confirmationAttempts; index += 1) {
    const baselineReport = await runBaseline(config.baseline, { baseUrl });
    const report = options.skipClientAssetChecks
      ? baselineReport
      : await appendClientAssetChecks(baselineReport, config, baseUrl, options.fetchImplementation ?? globalThis.fetch);
    attempts.push(report);
    if (report.results.every((result) => result.failures.length === 0)) {
      break;
    }
    if (index + 1 < config.confirmationAttempts) {
      await pause(config.confirmationDelayMilliseconds);
    }
  }

  const incidents = confirmIncidents(config, attempts);
  return {
    schemaVersion: 1,
    checkedAtUtc: new Date().toISOString(),
    baseUrl,
    status: incidents.length === 0 ? 'healthy' : 'incident',
    incidents,
    attempts,
  };
}

function buildSummary(report) {
  const lines = [
    '## Surveillance de production',
    '',
    `État : **${report.status === 'healthy' ? 'sain' : 'incident confirmé'}**`,
    '',
    `Cible : \`${report.baseUrl}\``,
    `Tentatives : ${report.attempts.length}`,
  ];

  for (const incident of report.incidents) {
    lines.push('', `- \`${incident.targetKey}\` — runbook \`${incident.incidentId}\``);
  }
  lines.push('');
  return `${lines.join('\n')}\n`;
}

async function runCli() {
  const configPath = resolve(process.env.PRODUCTION_ALERT_CONFIG ?? defaultConfigPath);
  const config = JSON.parse(await readFile(configPath, 'utf8'));
  const report = await runConfirmedProductionProbe(config, {
    baseUrl: process.env.PRODUCTION_ALERT_BASE_URL,
  });
  const outputPath = process.env.PRODUCTION_ALERT_OUTPUT;
  const serialized = `${JSON.stringify(report, null, 2)}\n`;

  if (outputPath) {
    await writeFile(resolve(outputPath), serialized, 'utf8');
  }
  if (process.env.GITHUB_STEP_SUMMARY) {
    await appendFile(process.env.GITHUB_STEP_SUMMARY, buildSummary(report), 'utf8');
  }

  process.stdout.write(serialized);
  if (report.status === 'incident') {
    process.exitCode = 1;
  }
}

const invokedPath = process.argv[1] ? pathToFileURL(resolve(process.argv[1])).href : null;
if (invokedPath === import.meta.url) {
  await runCli();
}
