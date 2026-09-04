import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  collectClassFileViolations,
  findNewOrWorsenedViolations,
  findStaleBaselineEntries,
  summarizeViolations,
} from './one-class-per-file.mjs';

const architectureDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(architectureDirectory, '../../../..');
const baselinePath = resolve(architectureDirectory, 'one-class-per-file-baseline.json');
const baselineRepositoryPath = relative(repositoryRoot, baselinePath).replaceAll('\\', '/');
const currentViolations = collectClassFileViolations(repositoryRoot);

function resolveComparisonCommit() {
  const workflowBaseSha = process.env.ONE_CLASS_BASE_SHA?.trim() ?? '';
  if (workflowBaseSha.length > 0 && !/^[0-9a-f]{40}$/iu.test(workflowBaseSha)) {
    throw new Error('ONE_CLASS_BASE_SHA must be a full Git commit SHA.');
  }
  if (/^[0]+$/u.test(workflowBaseSha)) {
    return null;
  }
  if (workflowBaseSha.length > 0) {
    return workflowBaseSha;
  }

  return execFileSync('git', ['rev-parse', 'HEAD^'], {
    cwd: repositoryRoot,
    encoding: 'utf8',
  }).trim();
}

function readComparisonBaseline() {
  const comparisonCommit = resolveComparisonCommit();
  if (comparisonCommit === null) {
    return null;
  }

  try {
    const content = execFileSync('git', ['show', `${comparisonCommit}:${baselineRepositoryPath}`], {
      cwd: repositoryRoot,
      encoding: 'utf8',
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    return JSON.parse(content);
  } catch (error) {
    const message = error?.stderr?.toString() ?? '';
    if (error?.status === 128 && /does not exist|exists on disk, but not in/iu.test(message)) {
      return null;
    }
    throw error;
  }
}

if (process.argv.includes('--write-baseline')) {
  const entries = currentViolations.map((violation) => `  ${JSON.stringify(violation)}`);
  writeFileSync(baselinePath, `[\n${entries.join(',\n')}\n]\n`, 'utf8');
  const summary = summarizeViolations(currentViolations);
  console.log(
    `Baseline written with ${summary.csharp} C# and ${summary.typescript} TypeScript non-compliant files.`,
  );
  process.exit(0);
}

if (!existsSync(baselinePath)) {
  console.error('One-class-per-file baseline is missing.');
  process.exit(1);
}

const baselineViolations = JSON.parse(readFileSync(baselinePath, 'utf8'));
const regressions = findNewOrWorsenedViolations(currentViolations, baselineViolations);
const staleBaselineEntries = findStaleBaselineEntries(currentViolations, baselineViolations);
const comparisonBaseline = readComparisonBaseline();
const baselineExpansions = comparisonBaseline === null
  ? []
  : findNewOrWorsenedViolations(baselineViolations, comparisonBaseline);

if (regressions.length > 0) {
  console.error('One-class-per-file architecture check failed:');
  for (const violation of regressions) {
    console.error(`- ${violation.path}: ${violation.classes.join(', ')}`);
  }
  console.error('Move every class to its own dedicated file.');
  process.exit(1);
}

if (baselineExpansions.length > 0) {
  console.error('One-class-per-file baseline cannot be expanded:');
  for (const violation of baselineExpansions) {
    console.error(`- ${violation.path}: ${violation.classes.join(', ')}`);
  }
  process.exit(1);
}

if (staleBaselineEntries.length > 0) {
  console.error('One-class-per-file baseline must shrink with the code:');
  for (const violation of staleBaselineEntries) {
    console.error(`- ${violation.path}`);
  }
  console.error('Regenerate the baseline and verify that it only removes resolved debt.');
  process.exit(1);
}

const summary = summarizeViolations(currentViolations);
console.log(
  `One-class-per-file architecture check passed; debt remaining: ${summary.csharp} C# and ${summary.typescript} TypeScript files.`,
);
