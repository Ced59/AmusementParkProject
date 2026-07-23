import { readFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';

export function classifyNpmAuditReport(reportText, auditExitCode) {
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

  if (high > 0 || critical > 0) {
    return {
      kind: 'vulnerabilities',
      message: `npm audit detected ${high} high and ${critical} critical vulnerabilities.`
    };
  }

  return { kind: 'clean', message: 'npm audit detected no high or critical vulnerabilities.' };
}

function readCount(value) {
  return Number.isInteger(value) && value >= 0 ? value : null;
}

async function runCli() {
  const reportPath = process.argv[2];
  const auditExitCode = Number.parseInt(process.argv[3] ?? '', 10);

  if (!reportPath || !Number.isInteger(auditExitCode)) {
    console.error('Usage: node classify-npm-audit-report.mjs <report.json> <audit-exit-code>');
    process.exitCode = 2;
    return;
  }

  let reportText;
  try {
    reportText = await readFile(reportPath, 'utf8');
  } catch {
    console.error(`Unable to read npm audit report: ${reportPath}`);
    process.exitCode = 2;
    return;
  }

  const classification = classifyNpmAuditReport(reportText, auditExitCode);
  console.log(classification.message);
  process.exitCode = classification.kind === 'clean' ? 0 : classification.kind === 'vulnerabilities' ? 1 : 2;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  await runCli();
}
