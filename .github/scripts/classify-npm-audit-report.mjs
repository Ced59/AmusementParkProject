import { readFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';

export function classifyNpmAuditReport(reportText, auditExitCode, options = {}) {
  if (auditExitCode !== 0 && auditExitCode !== 1) {
    return { kind: 'scan-error', message: `npm audit exited with unexpected code ${auditExitCode}.` };
  }

  let report;
  try {
    report = JSON.parse(reportText);
  } catch {
    return { kind: 'scan-error', message: 'npm audit did not return valid JSON.' };
  }

  if (report === null || typeof report !== 'object' || report.error !== undefined) {
    return { kind: 'scan-error', message: 'npm audit returned a registry or scanner error.' };
  }

  const vulnerabilities = report.metadata?.vulnerabilities;
  const high = readCount(vulnerabilities?.high);
  const critical = readCount(vulnerabilities?.critical);

  if (high === null || critical === null) {
    return { kind: 'scan-error', message: 'npm audit returned an incomplete vulnerability summary.' };
  }

  if (critical > 0) {
    return {
      kind: 'vulnerabilities',
      message: `npm audit detected ${high} high and ${critical} critical vulnerabilities.`
    };
  }

  if (high > 0) {
    const exceptionResult = classifyHighVulnerabilities(report, options);
    if (exceptionResult.kind !== 'allowed') {
      return exceptionResult;
    }

    return {
      kind: 'clean-with-exceptions',
      message: `npm audit detected ${high} high vulnerabilities covered by active development-only exception ${exceptionResult.exceptionIds.join(', ')}.`
    };
  }

  return { kind: 'clean', message: 'npm audit detected no high or critical vulnerabilities.' };
}

function classifyHighVulnerabilities(report, options) {
  const parsedInputs = parseExceptionInputs(options);
  if (parsedInputs.kind === 'scan-error') {
    return parsedInputs;
  }

  const vulnerabilities = report.vulnerabilities;
  if (vulnerabilities === null || typeof vulnerabilities !== 'object') {
    return { kind: 'scan-error', message: 'npm audit did not describe its high vulnerabilities.' };
  }

  const highPackages = Object.entries(vulnerabilities)
    .filter(([, vulnerability]) => vulnerability?.severity === 'high')
    .map(([packageName]) => packageName);
  if (highPackages.length === 0) {
    return { kind: 'scan-error', message: 'npm audit reported high vulnerabilities without package details.' };
  }

  const usedExceptions = new Set();
  for (const packageName of highPackages) {
    const trace = traceVulnerability(packageName, vulnerabilities, new Set());
    if (trace.kind === 'scan-error') {
      return trace;
    }

    for (const node of trace.nodes) {
      if (parsedInputs.packageLock.packages?.[node]?.dev !== true) {
        return {
          kind: 'vulnerabilities',
          message: `npm audit detected a high vulnerability reachable outside development dependencies at ${node}.`
        };
      }
    }

    for (const advisory of trace.advisories) {
      const matchingException = parsedInputs.exceptions.find((exception) =>
        exception.advisorySource === advisory.source
        && exception.package === advisory.name
        && exception.ghsaId.toUpperCase() === readGhsaId(advisory.url));
      if (!matchingException) {
        return {
          kind: 'vulnerabilities',
          message: `npm audit detected a high vulnerability without an active exception in ${packageName}.`
        };
      }

      usedExceptions.add(matchingException.ghsaId);
    }
  }

  return { kind: 'allowed', exceptionIds: [...usedExceptions].sort() };
}

function parseExceptionInputs(options) {
  if (typeof options.exceptionsText !== 'string' || typeof options.packageLockText !== 'string') {
    return { kind: 'scan-error', message: 'High vulnerability classification requires the exception policy and package lock.' };
  }

  let exceptionPolicy;
  let packageLock;
  try {
    exceptionPolicy = JSON.parse(options.exceptionsText);
    packageLock = JSON.parse(options.packageLockText);
  } catch {
    return { kind: 'scan-error', message: 'The npm audit exception policy or package lock is not valid JSON.' };
  }

  if (!Array.isArray(exceptionPolicy.exceptions) || packageLock === null || typeof packageLock !== 'object') {
    return { kind: 'scan-error', message: 'The npm audit exception policy or package lock has an invalid shape.' };
  }

  const currentDate = options.currentDate ?? new Date().toISOString().slice(0, 10);
  if (!isIsoDate(currentDate)) {
    return { kind: 'scan-error', message: 'The npm audit exception evaluation date is invalid.' };
  }

  const exceptions = [];
  for (const exception of exceptionPolicy.exceptions) {
    if (!isValidException(exception)) {
      return { kind: 'scan-error', message: 'The npm audit exception policy contains an invalid entry.' };
    }
    if (exception.expiresOn < currentDate) {
      continue;
    }
    exceptions.push(exception);
  }

  return { kind: 'parsed', exceptions, packageLock };
}

function isValidException(exception) {
  return exception !== null
    && typeof exception === 'object'
    && Number.isInteger(exception.advisorySource)
    && /^GHSA-[a-z0-9-]+$/i.test(exception.ghsaId)
    && typeof exception.package === 'string'
    && exception.package.length > 0
    && exception.severity === 'high'
    && exception.scope === 'development-only'
    && isIsoDate(exception.expiresOn)
    && typeof exception.reason === 'string'
    && exception.reason.length > 0;
}

function isIsoDate(value) {
  if (typeof value !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return false;
  }

  const parsedDate = new Date(`${value}T00:00:00.000Z`);
  return !Number.isNaN(parsedDate.getTime()) && parsedDate.toISOString().slice(0, 10) === value;
}

function traceVulnerability(packageName, vulnerabilities, visited) {
  if (visited.has(packageName)) {
    return { kind: 'scan-error', message: `npm audit returned a cyclic vulnerability graph at ${packageName}.` };
  }

  const vulnerability = vulnerabilities[packageName];
  if (vulnerability === null || typeof vulnerability !== 'object' || !Array.isArray(vulnerability.via)) {
    return { kind: 'scan-error', message: `npm audit returned incomplete vulnerability details for ${packageName}.` };
  }

  if (!Array.isArray(vulnerability.nodes) || vulnerability.nodes.length === 0
    || vulnerability.nodes.some((node) => typeof node !== 'string' || node.length === 0)) {
    return { kind: 'scan-error', message: `npm audit did not identify affected installations for ${packageName}.` };
  }

  const nextVisited = new Set(visited);
  nextVisited.add(packageName);
  const nodes = new Set(vulnerability.nodes);
  const advisories = [];

  for (const cause of vulnerability.via) {
    if (typeof cause === 'string') {
      const nested = traceVulnerability(cause, vulnerabilities, nextVisited);
      if (nested.kind === 'scan-error') {
        return nested;
      }
      nested.nodes.forEach((node) => nodes.add(node));
      advisories.push(...nested.advisories);
      continue;
    }

    if (cause === null || typeof cause !== 'object' || !Number.isInteger(cause.source)
      || typeof cause.name !== 'string' || typeof cause.url !== 'string') {
      return { kind: 'scan-error', message: `npm audit returned an invalid advisory for ${packageName}.` };
    }
    advisories.push(cause);
  }

  if (advisories.length === 0) {
    return { kind: 'scan-error', message: `npm audit did not identify the advisory behind ${packageName}.` };
  }

  return { kind: 'trace', nodes, advisories };
}

function readGhsaId(url) {
  return /GHSA-[a-z0-9-]+/i.exec(url)?.[0]?.toUpperCase() ?? null;
}

function readCount(value) {
  return Number.isInteger(value) && value >= 0 ? value : null;
}

async function runCli() {
  const reportPath = process.argv[2];
  const auditExitCode = Number.parseInt(process.argv[3] ?? '', 10);
  const packageLockPath = process.argv[4];
  const exceptionsPath = process.argv[5];

  if (!reportPath || !Number.isInteger(auditExitCode) || !packageLockPath || !exceptionsPath) {
    console.error('Usage: node classify-npm-audit-report.mjs <report.json> <audit-exit-code> <package-lock.json> <exceptions.json>');
    process.exitCode = 2;
    return;
  }

  let reportText;
  let packageLockText;
  let exceptionsText;
  try {
    reportText = await readFile(reportPath, 'utf8');
    packageLockText = await readFile(packageLockPath, 'utf8');
    exceptionsText = await readFile(exceptionsPath, 'utf8');
  } catch {
    console.error(`Unable to read npm audit report: ${reportPath}`);
    process.exitCode = 2;
    return;
  }

  const classification = classifyNpmAuditReport(reportText, auditExitCode, { packageLockText, exceptionsText });
  console.log(classification.message);
  process.exitCode = classification.kind === 'clean' || classification.kind === 'clean-with-exceptions'
    ? 0
    : classification.kind === 'vulnerabilities' ? 1 : 2;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  await runCli();
}
