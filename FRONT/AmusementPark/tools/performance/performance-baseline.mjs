import { readFile, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { performance } from 'node:perf_hooks';

const toolDirectory = dirname(fileURLToPath(import.meta.url));
const defaultConfigPath = resolve(toolDirectory, 'performance-baseline.config.json');

export function classifyHttpOutcome(status) {
  if (status >= 500) {
    return 'server_error';
  }

  if (status >= 400) {
    return 'client_error';
  }

  if (status >= 300) {
    return 'redirection';
  }

  return 'success';
}

export function nearestRankPercentile(values, percentile) {
  if (values.length === 0) {
    return null;
  }

  const sorted = [...values].sort((left, right) => left - right);
  const rank = Math.max(1, Math.ceil((percentile / 100) * sorted.length));
  return sorted[Math.min(rank - 1, sorted.length - 1)];
}

export function summarizeSamples(target, samples) {
  const successfulDurations = samples
    .filter((sample) => sample.outcome === 'success')
    .map((sample) => sample.durationMilliseconds);
  const statuses = {};
  const outcomes = {};

  for (const sample of samples) {
    const statusKey = sample.status === null ? 'none' : String(sample.status);
    statuses[statusKey] = (statuses[statusKey] ?? 0) + 1;
    outcomes[sample.outcome] = (outcomes[sample.outcome] ?? 0) + 1;
  }

  return {
    key: target.key,
    path: target.path,
    kind: target.kind,
    sampleCount: samples.length,
    p50Milliseconds: nearestRankPercentile(successfulDurations, 50),
    p95Milliseconds: nearestRankPercentile(successfulDurations, 95),
    maximumBytes: samples.reduce((maximum, sample) => Math.max(maximum, sample.bytes), 0),
    statuses,
    outcomes,
    buildVersions: [...new Set(samples.map((sample) => sample.buildVersion).filter(Boolean))],
    ssrModes: [...new Set(samples.map((sample) => sample.ssrMode).filter(Boolean))],
    missingSsrModeCount: samples.filter((sample) => !sample.ssrMode).length,
    seoReadiness: [...new Set(samples.map((sample) => sample.seoReady).filter(Boolean))],
    missingSeoReadyCount: samples.filter((sample) => !sample.seoReady).length,
  };
}

export function evaluateSummary(target, summary) {
  const failures = [];
  const unexpectedStatusCount = Object.entries(summary.statuses)
    .filter(([status]) => status !== String(target.expectedStatus))
    .reduce((total, [, count]) => total + count, 0);

  if (unexpectedStatusCount > 0) {
    failures.push(`${unexpectedStatusCount} réponse(s) avec un statut inattendu`);
  }

  if ((summary.outcomes.timeout ?? 0) > 0 || (summary.outcomes.transport_error ?? 0) > 0) {
    failures.push('erreur de transport ou expiration observée');
  }

  if (summary.p95Milliseconds === null || summary.p95Milliseconds > target.maximumP95Milliseconds) {
    failures.push(`p95 supérieur au budget de ${target.maximumP95Milliseconds} ms`);
  }

  if (summary.maximumBytes > target.maximumBytes) {
    failures.push(`réponse supérieure au budget de ${target.maximumBytes} octets`);
  }

  return failures;
}

async function measureOnce(baseUrl, target, timeoutMilliseconds, fetchImplementation) {
  const url = new URL(target.path, baseUrl);
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMilliseconds);
  const startedAt = performance.now();

  try {
    const response = await fetchImplementation(url, {
      headers: {
        accept: target.kind === 'api' ? 'application/json' : 'text/html',
        'user-agent': 'AmusementParkPerformanceBaseline/1.0',
      },
      redirect: 'manual',
      signal: controller.signal,
    });
    const body = await response.arrayBuffer();
    return {
      status: response.status,
      outcome: classifyHttpOutcome(response.status),
      durationMilliseconds: Math.round((performance.now() - startedAt) * 100) / 100,
      bytes: body.byteLength,
      buildVersion: response.headers.get('x-amusementpark-build-version'),
      ssrMode: response.headers.get('x-amusementpark-ssr-mode'),
      seoReady: response.headers.get('x-amusementpark-seo-ready')?.toLowerCase() ?? null,
    };
  } catch (error) {
    const isTimeout = controller.signal.aborted;
    return {
      status: null,
      outcome: isTimeout ? 'timeout' : 'transport_error',
      durationMilliseconds: Math.round((performance.now() - startedAt) * 100) / 100,
      bytes: 0,
      buildVersion: null,
      ssrMode: null,
      seoReady: null,
      error: error instanceof Error ? error.name : 'UnknownError',
    };
  } finally {
    clearTimeout(timeout);
  }
}

async function pause(milliseconds) {
  await new Promise((resolvePromise) => setTimeout(resolvePromise, milliseconds));
}

export async function runPerformanceBaseline(config, options = {}) {
  const baseUrl = options.baseUrl ?? 'https://amusement-parks.fun';
  const fetchImplementation = options.fetchImplementation ?? globalThis.fetch;
  const results = [];

  for (const target of config.targets) {
    for (let index = 0; index < config.warmupSamples; index += 1) {
      await measureOnce(baseUrl, target, config.timeoutMilliseconds, fetchImplementation);
      await pause(config.pauseMilliseconds);
    }

    const samples = [];
    for (let index = 0; index < config.samples; index += 1) {
      samples.push(await measureOnce(baseUrl, target, config.timeoutMilliseconds, fetchImplementation));
      if (index + 1 < config.samples) {
        await pause(config.pauseMilliseconds);
      }
    }

    const summary = summarizeSamples(target, samples);
    results.push({ ...summary, failures: evaluateSummary(target, summary) });
  }

  return {
    schemaVersion: config.schemaVersion,
    measuredAtUtc: new Date().toISOString(),
    baseUrl,
    method: {
      samples: config.samples,
      warmupSamples: config.warmupSamples,
      pauseMilliseconds: config.pauseMilliseconds,
      timeoutMilliseconds: config.timeoutMilliseconds,
      percentile: 'nearest-rank',
      cache: 'public behavior preserved',
    },
    results,
  };
}

async function runCli() {
  const configPath = resolve(process.env.PERFORMANCE_BASELINE_CONFIG ?? defaultConfigPath);
  const config = JSON.parse(await readFile(configPath, 'utf8'));
  const report = await runPerformanceBaseline(config, {
    baseUrl: process.env.PERFORMANCE_BASE_URL,
  });
  const serializedReport = `${JSON.stringify(report, null, 2)}\n`;
  const outputPath = process.env.PERFORMANCE_BASELINE_OUTPUT;

  if (outputPath) {
    await writeFile(resolve(outputPath), serializedReport, 'utf8');
  }

  process.stdout.write(serializedReport);

  if (process.argv.includes('--enforce') && report.results.some((result) => result.failures.length > 0)) {
    process.exitCode = 1;
  }
}

const invokedPath = process.argv[1] ? pathToFileURL(resolve(process.argv[1])).href : null;
if (invokedPath === import.meta.url) {
  await runCli();
}
