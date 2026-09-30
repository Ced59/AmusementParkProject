import { appendFile, readFile, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

import { runPerformanceBaseline } from '../performance/performance-baseline.mjs';

const toolDirectory = dirname(fileURLToPath(import.meta.url));
const defaultConfigPath = resolve(toolDirectory, 'production-alerts.config.json');

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
  const attempts = [];

  for (let index = 0; index < config.confirmationAttempts; index += 1) {
    const report = await runBaseline(config.baseline, { baseUrl: options.baseUrl });
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
    baseUrl: options.baseUrl ?? 'https://amusement-parks.fun',
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
