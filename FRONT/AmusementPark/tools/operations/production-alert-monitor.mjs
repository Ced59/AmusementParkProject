import { appendFile, readFile, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import typescript from 'typescript';

import { runPerformanceBaseline } from '../performance/performance-baseline.mjs';

const toolDirectory = dirname(fileURLToPath(import.meta.url));
const defaultConfigPath = resolve(toolDirectory, 'production-alerts.config.json');
const maximumClientAssets = 25;
const maximumStaticModuleAssets = 64;
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

export function extractStaticModuleDependencies(assetUrl, source) {
  const origin = new URL(assetUrl).origin;
  const sourceFile = typescript.createSourceFile(
    new URL(assetUrl).pathname,
    source,
    typescript.ScriptTarget.Latest,
    false,
    typescript.ScriptKind.JS,
  );
  const dependencies = new Map();

  const registerDependency = (moduleSpecifier, requiredExports) => {
    if (!moduleSpecifier.endsWith('.js')) {
      return;
    }
    const resolved = new URL(moduleSpecifier, assetUrl);
    if (resolved.origin !== origin) {
      return;
    }
    const current = dependencies.get(resolved.href) ?? new Set();
    for (const exportName of requiredExports) {
      current.add(exportName);
    }
    dependencies.set(resolved.href, current);
  };

  for (const statement of sourceFile.statements) {
    if (typescript.isImportDeclaration(statement)
      && typescript.isStringLiteralLike(statement.moduleSpecifier)) {
      const clause = statement.importClause;
      const requiredExports = [];
      if (clause?.name) {
        requiredExports.push('default');
      }
      if (clause?.namedBindings && typescript.isNamedImports(clause.namedBindings)) {
        for (const element of clause.namedBindings.elements) {
          requiredExports.push((element.propertyName ?? element.name).text);
        }
      }
      registerDependency(statement.moduleSpecifier.text, requiredExports);
    } else if (typescript.isExportDeclaration(statement)
      && statement.moduleSpecifier
      && typescript.isStringLiteralLike(statement.moduleSpecifier)) {
      const requiredExports = [];
      if (statement.exportClause && typescript.isNamedExports(statement.exportClause)) {
        for (const element of statement.exportClause.elements) {
          requiredExports.push((element.propertyName ?? element.name).text);
        }
      }
      registerDependency(statement.moduleSpecifier.text, requiredExports);
    }
  }

  return [...dependencies].map(([url, requiredExports]) => ({
    url,
    requiredExports: [...requiredExports],
  }));
}

export function extractStaticModuleAssetUrls(assetUrl, source) {
  return extractStaticModuleDependencies(assetUrl, source).map((dependency) => dependency.url);
}

export function extractModuleExportNames(assetUrl, source) {
  const sourceFile = typescript.createSourceFile(
    new URL(assetUrl).pathname,
    source,
    typescript.ScriptTarget.Latest,
    false,
    typescript.ScriptKind.JS,
  );
  const exportNames = new Set();

  for (const statement of sourceFile.statements) {
    if (typescript.isExportAssignment(statement)) {
      exportNames.add('default');
      continue;
    }
    if (typescript.isExportDeclaration(statement)) {
      if (statement.exportClause && typescript.isNamedExports(statement.exportClause)) {
        for (const element of statement.exportClause.elements) {
          exportNames.add(element.name.text);
        }
      } else if (typescript.isNamespaceExport(statement.exportClause)) {
        exportNames.add(statement.exportClause.name.text);
      }
      continue;
    }

    const modifiers = typescript.canHaveModifiers(statement)
      ? typescript.getModifiers(statement) ?? []
      : [];
    if (!modifiers.some((modifier) => modifier.kind === typescript.SyntaxKind.ExportKeyword)) {
      continue;
    }
    if (modifiers.some((modifier) => modifier.kind === typescript.SyntaxKind.DefaultKeyword)) {
      exportNames.add('default');
    } else if ('name' in statement && statement.name?.text) {
      exportNames.add(statement.name.text);
    } else if (typescript.isVariableStatement(statement)) {
      for (const declaration of statement.declarationList.declarations) {
        if (typescript.isIdentifier(declaration.name)) {
          exportNames.add(declaration.name.text);
        }
      }
    }
  }

  return exportNames;
}

async function probeJavaScriptAsset(
  assetUrl,
  fetchImplementation,
  timeoutMilliseconds,
) {
  try {
    const { response, body: source } = await fetchTextWithTimeout(fetchImplementation, assetUrl, {
      headers: { accept: 'text/javascript, application/javascript', 'user-agent': 'AmusementParkProductionMonitor/1.0' },
    }, timeoutMilliseconds);
    const contentType = response.headers.get('content-type')?.toLowerCase() ?? '';
    if (!response.ok || !contentType.includes('javascript')) {
      return { failure: `${new URL(assetUrl).pathname}: bundle client invalide`, source: null };
    }
    const sourceFile = typescript.createSourceFile(
      new URL(assetUrl).pathname,
      source,
      typescript.ScriptTarget.Latest,
      false,
      typescript.ScriptKind.JS,
    );
    if (sourceFile.parseDiagnostics.length > 0) {
      return { failure: `${new URL(assetUrl).pathname}: syntaxe JavaScript invalide`, source: null };
    }
    return { failure: null, source };
  } catch {
    return { failure: `${new URL(assetUrl).pathname}: bundle client inaccessible ou expiré`, source: null };
  }
}

async function probeJavaScriptAssets(
  assetUrls,
  fetchImplementation,
  timeoutMilliseconds,
) {
  const results = new Map();
  for (let index = 0; index < assetUrls.length; index += clientAssetConcurrency) {
    const batch = assetUrls.slice(index, index + clientAssetConcurrency);
    const batchResults = await Promise.all(batch.map(async (assetUrl) => ({
      assetUrl,
      result: await probeJavaScriptAsset(
        assetUrl,
        fetchImplementation,
        timeoutMilliseconds,
      ),
    })));
    for (const item of batchResults) {
      results.set(item.assetUrl, item.result);
    }
  }
  return results;
}

async function probeStaticModuleGraph(sources, fetchImplementation, timeoutMilliseconds) {
  const seen = new Set(sources.keys());
  const pending = [];
  const failures = [];
  const requiredExportsByModule = new Map();

  const enqueueImports = (assetUrl, source) => {
    for (const dependency of extractStaticModuleDependencies(assetUrl, source)) {
      const requiredExports = requiredExportsByModule.get(dependency.url) ?? new Set();
      for (const exportName of dependency.requiredExports) {
        requiredExports.add(exportName);
      }
      requiredExportsByModule.set(dependency.url, requiredExports);
      if (!seen.has(dependency.url) && !pending.includes(dependency.url)) {
        pending.push(dependency.url);
      }
    }
  };
  for (const [assetUrl, source] of sources) {
    enqueueImports(assetUrl, source);
  }

  let discoveredCount = 0;
  while (pending.length > 0) {
    if (discoveredCount >= maximumStaticModuleAssets) {
      failures.push(`graphe des imports statiques supérieur à ${maximumStaticModuleAssets} bundles`);
      break;
    }

    const remainingBudget = maximumStaticModuleAssets - discoveredCount;
    const batch = pending.splice(0, Math.min(clientAssetConcurrency, remainingBudget));
    const results = await probeJavaScriptAssets(
      batch,
      fetchImplementation,
      timeoutMilliseconds,
    );
    discoveredCount += batch.length;
    for (const assetUrl of results.keys()) {
      seen.add(assetUrl);
    }
    for (const [assetUrl, result] of results) {
      if (result.failure) {
        failures.push(result.failure);
        continue;
      }
      sources.set(assetUrl, result.source);
      enqueueImports(assetUrl, result.source);
    }
  }

  for (const [assetUrl, requiredExports] of requiredExportsByModule) {
    const source = sources.get(assetUrl);
    if (source === undefined || requiredExports.size === 0) {
      continue;
    }
    if (source.length === 0) {
      failures.push(`${new URL(assetUrl).pathname}: module vide malgré des exports requis`);
      continue;
    }
    const availableExports = extractModuleExportNames(assetUrl, source);
    const missingExports = [...requiredExports].filter((exportName) => !availableExports.has(exportName));
    if (missingExports.length > 0) {
      failures.push(`${new URL(assetUrl).pathname}: export(s) requis absent(s): ${missingExports.join(', ')}`);
    }
  }

  return { discoveredCount, failures };
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
      } else if (result.source.length === 0) {
        failures.push(`${new URL(assetUrl).pathname}: bundle client vide`);
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
        } else if (lazyResult.source.length === 0) {
          failures.push(`${new URL(lazyRouteAssetUrl).pathname}: bundle client vide`);
        } else {
          sources.set(lazyRouteAssetUrl, lazyResult.source);
        }
      }
    }

    if (target.clientRoutePath && failures.length === 0) {
      const lazyRouteSource = sources.get(lazyRouteAssetUrl);
      const routeExports = extractModuleExportNames(lazyRouteAssetUrl, lazyRouteSource);
      if (!routeExports.has(target.clientRouteExport)) {
        failures.push(
          `${new URL(lazyRouteAssetUrl).pathname}: export de route absent: ${target.clientRouteExport}`,
        );
      }
    }

    if (target.clientRoutePath && failures.length === 0) {
      const graph = await probeStaticModuleGraph(sources, fetchImplementation, timeoutMilliseconds);
      failures.push(...graph.failures);
    }

    return {
      assetCount: sources.size,
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

export function appendSsrModeChecks(report, config) {
  for (const target of config.baseline.targets.filter((candidate) => candidate.kind === 'ssr-page')) {
    const result = report.results.find((candidate) => candidate.key === target.key);
    if (!result) {
      continue;
    }

    const unexpectedModes = result.ssrModes
      .filter((mode) => !target.allowedSsrModes.includes(mode));
    if (result.ssrModes.length === 0 || result.missingSsrModeCount > 0) {
      result.failures.push('mode SSR absent de la réponse publique');
    } else if (unexpectedModes.length > 0) {
      result.failures.push(`mode SSR inattendu: ${unexpectedModes.join(', ')}`);
    }

    const seoReadiness = result.seoReadiness ?? [];
    const missingSeoReadyCount = result.missingSeoReadyCount ?? result.sampleCount ?? 1;
    if (seoReadiness.length === 0 || missingSeoReadyCount > 0) {
      result.failures.push('preuve SEO absente de la réponse publique');
    } else if (seoReadiness.some((value) => value !== 'true')) {
      result.failures.push(`réponse SSR non prête pour le SEO: ${seoReadiness.join(', ')}`);
    }
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
    appendSsrModeChecks(baselineReport, config);
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
